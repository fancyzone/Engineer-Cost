using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using 施工定额.Entity;

namespace 施工定额.Export
{
    /// <summary>
    /// 导出时需要人工填写的项目级元数据。
    /// 这些是标准里标记为"必填"、但你现有数据模型里没有存储的字段，
    /// 暂时通过一个简单的表单/对话框让用户填写，不需要存进数据库。
    /// </summary>
    public class YdjcProjectInfo
    {
        public string ProjectName { get; set; } = "";      // ConstructionProject.Name
        public string Segment { get; set; } = "";           // ConstructionProject.Segment（标段名称）
        public int FileKind { get; set; } = 3;               // 3=最高投标限价，按需调整
        public string Owner { get; set; } = "";              // ProjectInfo.Owner（建设单位）
        public string CompilerName { get; set; } = "";       // ProjectInfo.Name（编制单位）
        public DateTime CompileDate { get; set; } = DateTime.Today;
        public string UnitWorkName { get; set; } = "";       // 单位工程名称
        public string Scale { get; set; } = "";              // 建设工程规模，如 "20000 m2"
    }

    /// <summary>
    /// 河南省《建设工程工程造价成果数据交换标准》(.YDJC) 导出服务。
    ///
    /// 当前实现把整个应用的数据当作"一个单位工程"导出（程序目前也没有
    /// 单项工程/多单位工程的概念），如果以后需要支持多单位工程，
    /// 需要先在数据模型里加上对应的分组维度，这里再改造。
    /// </summary>
    public class YdjcExportService
    {
        private readonly IYdjcExportStrategy _strategy;

        public YdjcExportService(IYdjcExportStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>
        /// 导出为 .YDJC 文件。
        /// </summary>
        /// <param name="qingdanList">完整的清单树（含定额、消耗量）</param>
        /// <param name="info">项目级元数据</param>
        /// <param name="outputFilePath">输出文件完整路径，建议以 .YDJC 结尾</param>
        public void Export(List<Qingdan> qingdanList, YdjcProjectInfo info, string outputFilePath)
        {
            string unitWorkFileName = $"@_{Sanitize(info.UnitWorkName)}.xml";

            var projectsXml = BuildProjectsXml(qingdanList, info, unitWorkFileName);
            var unitWorkXml = BuildUnitWorkXml(qingdanList, info);

            string tempDir = Path.Combine(Path.GetTempPath(), "ydjc_export_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string projectsPath = Path.Combine(tempDir, "Projects.xml");
                string unitWorkPath = Path.Combine(tempDir, unitWorkFileName);

                SaveXml(projectsXml, projectsPath);
                SaveXml(unitWorkXml, unitWorkPath);

                // ZipFile.CreateFromDirectory 不会自动创建目标路径的父目录，
                // 需要手动确保它存在，否则会抛 DirectoryNotFoundException。
                string? outputDir = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(outputDir))
                    Directory.CreateDirectory(outputDir);

                if (File.Exists(outputFilePath))
                    File.Delete(outputFilePath);

                ZipFile.CreateFromDirectory(tempDir, outputFilePath, CompressionLevel.Optimal, includeBaseDirectory: false);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        // ── Projects.xml（标准第 4 章：建设项目）───────────────────
        private XDocument BuildProjectsXml(List<Qingdan> qingdanList, YdjcProjectInfo info, string unitWorkFileName)
        {
            decimal projectTotal = qingdanList.Sum(q => q.综合合价);

            var root = new XElement("ConstructionProject",
                new XAttribute("Name", info.ProjectName ?? ""),
                new XAttribute("Segment", info.Segment ?? ""),
                new XAttribute("FileKind", info.FileKind),
                new XAttribute("StandardName", _strategy.StandardName));

            root.Add(new XElement("SystemInfo",
                new XAttribute("ID1", "施工定额;施工定额;1.0;"),
                new XAttribute("ID2", ""),
                new XAttribute("MakeDate", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))));

            root.Add(new XElement("ProjectInfo",
                new XAttribute("Owner", info.Owner ?? ""),
                new XAttribute("Name", info.CompilerName ?? ""),
                new XAttribute("CompileDate", info.CompileDate.ToString("yyyy-MM-dd")),
                new XAttribute("ValuationMethod", _strategy.ValuationMethod),
                new XAttribute("TaxModel", _strategy.TaxModel),
                new XAttribute("Total", D(projectTotal))));

            root.Add(new XElement("TotalCosts",
                new XElement("CostItem",
                    new XAttribute("Number", "1"),
                    new XAttribute("Code", "FB_HJ"),
                    new XAttribute("Name", "分部分项合计"),
                    new XAttribute("Total", D(projectTotal)))));

            var projectfee = new XElement("Projectfee",
                new XAttribute("Name", info.ProjectName ?? ""),
                new XAttribute("Total", D(projectTotal)),
                new XAttribute("Scale", info.Scale ?? ""));

            projectfee.Add(BuildProjectCosts(projectTotal));

            projectfee.Add(new XElement("UnitWorks",
                new XAttribute("OrdCode", "1"),
                new XAttribute("RelFileName", unitWorkFileName),
                new XAttribute("Name", info.UnitWorkName ?? ""),
                new XAttribute("Total", D(projectTotal)),
                new XAttribute("Scale", info.Scale ?? "")));

            root.Add(projectfee);

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        }

        // ── 单位工程 XML（标准第 6 章）───────────────────────────
        private XDocument BuildUnitWorkXml(List<Qingdan> qingdanList, YdjcProjectInfo info)
        {
            decimal total = qingdanList.Sum(q => q.综合合价);

            var root = new XElement("UnitWorks",
                new XAttribute("OrdCode", "1"),
                new XAttribute("Name", info.UnitWorkName ?? ""),
                new XAttribute("Total", D(total)),
                new XAttribute("Scale", info.Scale ?? ""));

            root.Add(new XElement("Configure", _strategy.BuildDecimalConfig(), _strategy.BuildChargeTables()));

            root.Add(BuildSummary(qingdanList));

            var billTable = new XElement("BillTable");
            foreach (var qd in qingdanList)
                billTable.Add(_strategy.MapListProjects(qd));
            root.Add(billTable);

            root.Add(BuildResource(qingdanList));

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        }

        // ── 单位工程费汇总（6.4，简化：只输出分部分项合计）────────
        private XElement BuildSummary(List<Qingdan> qingdanList)
        {
            decimal total = qingdanList.Sum(q => q.综合合价);
            return new XElement("Summary",
                new XElement("SummaryItem",
                    new XAttribute("OrdCode", "1"),
                    new XAttribute("Name", "分部分项工程费"),
                    new XAttribute("KindCode", "1002"),
                    new XAttribute("Total", D(total))));
        }

        // ── 工程费汇总 ProjectCosts（标准 4.9.2，简化版）──────────
        private XElement BuildProjectCosts(decimal billTotal)
        {
            return new XElement("ProjectCosts",
                new XAttribute("Bill", D(billTotal)),
                new XAttribute("Preliminaries", D(0)),
                new XAttribute("PreliminariesByTotal", D(0)),
                new XAttribute("Safe", D(0)),
                new XAttribute("PreliminariesByPrice", D(0)),
                new XAttribute("OtherPreliminaries", D(0)),
                new XAttribute("Other", D(0)),
                new XAttribute("StatutoryFees", D(0)),  // TODO
                new XAttribute("Tax", D(0)),             // TODO
                new XAttribute("Labor", D(0)),           // TODO：如需精确，从 Costs 里累加
                new XAttribute("Material", D(0)),        // TODO
                new XAttribute("MainMaterial", D(0)),
                new XAttribute("Equipment", D(0)),
                new XAttribute("Machine", D(0)),         // TODO
                new XAttribute("Overhead", D(0)),        // TODO
                new XAttribute("Profit", D(0)));         // TODO
        }

        // ── 工料机汇总（标准 6.8，按消耗量编码全局去重合并）────────
        private XElement BuildResource(List<Qingdan> qingdanList)
        {
            var allXhl = qingdanList
                .SelectMany(q => q.定额列表)
                .SelectMany(d => d.消耗量列表)
                .ToList();

            var aggregates = allXhl
                .GroupBy(x => x.消耗量编码)
                .Select(g => new ResourceAggregate
                {
                    ResID = HenanYdjcExportStrategy.ResIdFor(g.Key),
                    消耗量编码 = g.Key,
                    消耗量名称 = g.First().消耗量名称,
                    规格型号 = g.First().规格型号,
                    消耗量单位 = g.First().消耗量单位,
                    消耗量类别 = g.First().消耗量类别,
                    定额基价 = g.Max(x => x.定额基价),
                    市场价 = g.Max(x => x.市场价),
                    数量合计 = g.Sum(x => x.数量),
                    定额价合价 = g.Sum(x => x.定额基价 * x.数量),
                    编制价合价 = g.Sum(x => x.市场价合计)
                })
                .ToList();

            var resource = new XElement("Resource");
            foreach (var agg in aggregates)
                resource.Add(_strategy.MapResourceItem(agg));

            return resource;
        }

        private static void SaveXml(XDocument doc, string path)
        {
            var settings = new System.Xml.XmlWriterSettings
            {
                Encoding = new System.Text.UTF8Encoding(false), // 不带 BOM
                Indent = true
            };
            using var writer = System.Xml.XmlWriter.Create(path, settings);
            doc.Save(writer);
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "UnitWork";
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Where(c => !invalid.Contains(c)).ToArray());
        }

        private static string D(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
