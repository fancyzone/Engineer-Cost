using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using 施工定额.Entity;
using 施工定额.Service;

namespace 施工定额.Export
{
    /// <summary>
    /// 导出时需要人工填写的项目级元数据。
    /// </summary>
    public class YdjcProjectInfo
    {
        public string ProjectName { get; set; } = "";
        public string Segment { get; set; } = "";
        public int FileKind { get; set; } = 3;
        public string Owner { get; set; } = "";
        public string CompilerName { get; set; } = "";
        public DateTime CompileDate { get; set; } = DateTime.Today;
        public string UnitWorkName { get; set; } = "";
        public string Scale { get; set; } = "";
    }

    /// <summary>
    /// 河南省《建设工程工程造价成果数据交换标准》(.YDJC) 导出服务。
    /// 项目级费用汇总使用 CostCalculationService.CalculateProjectSummary。
    /// </summary>
    public class YdjcExportService
    {
        private readonly IYdjcExportStrategy _strategy;
        private readonly CostCalculationService _calcService;

        public YdjcExportService(IYdjcExportStrategy strategy)
            : this(strategy, new CostCalculationService())
        {
        }

        public YdjcExportService(IYdjcExportStrategy strategy, CostCalculationService calcService)
        {
            _strategy = strategy;
            _calcService = calcService;
        }

        public void Export(List<Qingdan> qingdanList, YdjcProjectInfo info, string outputFilePath)
        {
            _calcService.RecalculateAll(qingdanList);

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

        private XDocument BuildProjectsXml(List<Qingdan> qingdanList, YdjcProjectInfo info, string unitWorkFileName)
        {
            var summary = _calcService.CalculateProjectSummary(qingdanList);

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
                new XAttribute("Total", D(summary.含税总价))));

            root.Add(new XElement("TotalCosts",
                new XElement("CostItem",
                    new XAttribute("Number", "1"),
                    new XAttribute("Code", "FB_HJ"),
                    new XAttribute("Name", "分部分项合计"),
                    new XAttribute("Total", D(summary.分部分项合价)))));

            var projectfee = new XElement("Projectfee",
                new XAttribute("Name", info.ProjectName ?? ""),
                new XAttribute("Total", D(summary.含税总价)),
                new XAttribute("Scale", info.Scale ?? ""));

            projectfee.Add(BuildProjectCosts(summary));

            projectfee.Add(new XElement("UnitWorks",
                new XAttribute("OrdCode", "1"),
                new XAttribute("RelFileName", unitWorkFileName),
                new XAttribute("Name", info.UnitWorkName ?? ""),
                new XAttribute("Total", D(summary.含税总价)),
                new XAttribute("Scale", info.Scale ?? "")));

            root.Add(projectfee);

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        }

        private XDocument BuildUnitWorkXml(List<Qingdan> qingdanList, YdjcProjectInfo info)
        {
            var summary = _calcService.CalculateProjectSummary(qingdanList);

            var root = new XElement("UnitWorks",
                new XAttribute("OrdCode", "1"),
                new XAttribute("Name", info.UnitWorkName ?? ""),
                new XAttribute("Total", D(summary.含税总价)),
                new XAttribute("Scale", info.Scale ?? ""));

            root.Add(new XElement("Configure", _strategy.BuildDecimalConfig(), _strategy.BuildChargeTables()));

            root.Add(BuildSummary(summary));

            var billTable = new XElement("BillTable");
            foreach (var qd in qingdanList)
                billTable.Add(_strategy.MapListProjects(qd));
            root.Add(billTable);

            root.Add(BuildResource(qingdanList));

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        }

        private XElement BuildSummary(ProjectCostSummary summary)
        {
            return new XElement("Summary",
                new XElement("SummaryItem",
                    new XAttribute("OrdCode", "1"),
                    new XAttribute("Name", "分部分项工程费"),
                    new XAttribute("KindCode", "1002"),
                    new XAttribute("Total", D(summary.分部分项合价))),
                new XElement("SummaryItem",
                    new XAttribute("OrdCode", "2"),
                    new XAttribute("Name", "规费"),
                    new XAttribute("KindCode", "1007"),
                    new XAttribute("Total", D(summary.规费))),
                new XElement("SummaryItem",
                    new XAttribute("OrdCode", "3"),
                    new XAttribute("Name", "税金"),
                    new XAttribute("KindCode", "1008"),
                    new XAttribute("Total", D(summary.税金))),
                new XElement("SummaryItem",
                    new XAttribute("OrdCode", "4"),
                    new XAttribute("Name", "含税总价"),
                    new XAttribute("KindCode", "1001"),
                    new XAttribute("Total", D(summary.含税总价))));
        }

        private XElement BuildProjectCosts(ProjectCostSummary summary)
        {
            return new XElement("ProjectCosts",
                new XAttribute("Bill", D(summary.分部分项合价)),
                new XAttribute("Preliminaries", D(0)),
                new XAttribute("PreliminariesByTotal", D(0)),
                new XAttribute("Safe", D(0)),
                new XAttribute("PreliminariesByPrice", D(0)),
                new XAttribute("OtherPreliminaries", D(0)),
                new XAttribute("Other", D(0)),
                new XAttribute("StatutoryFees", D(summary.规费)),
                new XAttribute("Tax", D(summary.税金)),
                new XAttribute("Labor", D(summary.人工费)),
                new XAttribute("Material", D(summary.材料费)),
                new XAttribute("MainMaterial", D(0)),
                new XAttribute("Equipment", D(0)),
                new XAttribute("Machine", D(summary.机械费)),
                new XAttribute("Overhead", D(summary.管理费)),
                new XAttribute("Profit", D(summary.利润)));
        }

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
                Encoding = new System.Text.UTF8Encoding(false),
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
