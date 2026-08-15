using 施工定额.Entity;
using 施工定额.Export;
using 施工定额.Helper;
using 施工定额.Service;

namespace 施工定额.UI
{
    /// <summary>
    /// 导出流程协调：项目信息窗体 → 另存为 → 调用导出服务。
    /// 从 Form1 拆出，避免 UI 事件方法继续膨胀。
    /// </summary>
    public static class ExportCoordinator
    {
        public static void ExportYdjc(
            IWin32Window owner,
            IList<Qingdan> qingdanList,
            ICostCalculationService calcService)
        {
            if (qingdanList == null || qingdanList.Count == 0)
            {
                ErrorHandler.ShowBusiness("当前没有可导出的清单数据。");
                return;
            }

            using var infoForm = new ExportProjectInfoForm();
            if (infoForm.ShowDialog(owner) != DialogResult.OK)
                return;

            using var sfd = new SaveFileDialog
            {
                Title = "导出 YDJC 文件",
                Filter = "YDJC 文件 (*.YDJC)|*.YDJC|所有文件 (*.*)|*.*",
                FileName = $"{infoForm.ProjectName}.YDJC",
                DefaultExt = "YDJC",
                AddExtension = true
            };

            if (sfd.ShowDialog(owner) != DialogResult.OK)
                return;

            try
            {
                if (owner is Control c)
                    c.Cursor = Cursors.WaitCursor;

                IYdjcExportStrategy strategy = new HenanYdjcExportStrategy();
                var exportService = new YdjcExportService(strategy, calcService);

                var info = new YdjcProjectInfo
                {
                    ProjectName = infoForm.ProjectName,
                    Owner = infoForm.Owner,
                    CompilerName = infoForm.CompilerName,
                    UnitWorkName = infoForm.UnitWorkName,
                    Scale = infoForm.Scale
                };

                exportService.Export(qingdanList.ToList(), info, sfd.FileName);
                AppLogger.Info($"导出成功: {sfd.FileName}");
                ErrorHandler.ShowBusiness($"导出成功：\n{sfd.FileName}", "导出完成");
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(ex, "导出失败");
            }
            finally
            {
                if (owner is Control c)
                    c.Cursor = Cursors.Default;
            }
        }
    }
}
