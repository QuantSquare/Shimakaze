// See https://aka.ms/new-console-template for more information
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

static Task generateResult(string directory, Provider provider)
{
    return Task.Run(() =>
    {
        var result = new StringBuilder();

        switch (provider)
        {
            case Provider.Jees:
                foreach (var file in Directory.EnumerateFiles(directory).Where(s => s.Split(Path.DirectorySeparatorChar)[2].StartsWith("结算单_")))
                {
                    Console.WriteLine($"Task={Task.CurrentId}, file={file}, Thread={Environment.CurrentManagedThreadId}");

                    foreach (var line in File.ReadLines(file).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号："))
                        {
                            var clientId = line.Split(new[] { "客户号：" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (clientId != file.Split(Path.DirectorySeparatorChar)[1].Split("_")[^1])
                            {
                                throw new InvalidOperationException();
                            }
                            _ = result.Append(clientId);
                        }
                        if (line.StartsWith("日期："))
                        {
                            _ = result.Append($",{line.Split(new[] { "日期：" }, StringSplitOptions.RemoveEmptyEntries)[0]}");
                        }
                        if (line.StartsWith("出入金Deposit/Withdrawal："))
                        {
                            var columns = line.Split(new[] { "出入金Deposit/Withdrawal：", "期末结存Balancec/f：" }, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",{columns[0]},{columns[1]}");
                        }
                        if (line.StartsWith("权利金收入Premiumreceived："))
                        {
                            _ = result.Append($",{line.Split(new[] { "权利金收入Premiumreceived：", "风险度RiskDegree：" }, StringSplitOptions.RemoveEmptyEntries)[1]}");
                        }
                    }
                    _ = result.AppendLine();
                }
                break;
            case Provider.Lanyee:
                throw new NotImplementedException();
            case Provider.Rohon:
                foreach (var file in Directory.EnumerateFiles(directory).Where(s => s.Split(Path.DirectorySeparatorChar)[2].StartsWith("20")))
                {
                    Console.WriteLine($"Task={Task.CurrentId}, file={file}, Thread={Environment.CurrentManagedThreadId}");

                    foreach (var line in File.ReadLines(file, Encoding.GetEncoding("GB18030")).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号："))
                        {
                            var clientId = line.Split(new[] { "客户号：", "客户名称：" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (clientId != file.Split(Path.DirectorySeparatorChar)[1].Split("_")[^1])
                            {
                                throw new InvalidOperationException();
                            }
                            _ = result.Append(clientId);
                        }
                        if (line.StartsWith("日期："))
                        {
                            _ = result.Append($",{line.Split(new[] { "日期：" }, StringSplitOptions.RemoveEmptyEntries)[0]}");
                        }
                        if (line.StartsWith("出入金："))
                        {
                            var columns = line.Split(new[] { "出入金：", "期末结存：", "可用资金：" }, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",{columns[0]},{columns[1]}");
                        }
                        if (line.StartsWith("平仓盈亏："))
                        {
                            _ = result.Append($",{line.Split(new[] { "平仓盈亏：", "质押金：", "风险度：" }, StringSplitOptions.RemoveEmptyEntries)[2]}");
                        }
                    }
                    _ = result.AppendLine();
                }
                break;
            case Provider.Shinny:
                foreach (var file in Directory.EnumerateFiles(directory).Where(s => s.Split(Path.DirectorySeparatorChar)[2].StartsWith("结算单_")))
                {
                    Console.WriteLine($"Task={Task.CurrentId}, file={file}, Thread={Environment.CurrentManagedThreadId}");

                    foreach (var line in File.ReadAllLines(file, Encoding.GetEncoding("GB18030")).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号ClientID："))
                        {
                            var clientId = line.Split(new[] { "客户号ClientID：", "客户名称ClientName：" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (clientId != file.Split(Path.DirectorySeparatorChar)[1].Split("_")[^1])
                            {
                                throw new InvalidOperationException();
                            }
                            _ = result.Append(clientId);
                        }
                        if (line.StartsWith("日期Date："))
                        {
                            _ = result.Append($",{line.Split(new[] { "日期Date：" }, StringSplitOptions.RemoveEmptyEntries)[0]}");
                        }
                        if (line.StartsWith("出入金Deposit/Withdrawal："))
                        {
                            var columns = line.Split(new[] { "出入金Deposit/Withdrawal：", "期末结存Balancec/f：" }, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",{columns[0]},{columns[1]}");
                        }
                        if (line.StartsWith("权利金收入Premiumreceived："))
                        {
                            _ = result.Append($",{line.Split(new[] { "权利金收入Premiumreceived：", "风险度RiskDegree：" }, StringSplitOptions.RemoveEmptyEntries)[1]}");
                        }
                    }
                    _ = result.AppendLine();
                }
                break;
            default:
                throw new NotImplementedException();
        }

        File.WriteAllLines($"Result_{directory.Split(Path.DirectorySeparatorChar)[1]}.csv", new[] { result.ToString() });
    });
}

List<Task>? tasks = new();

foreach (var directory in Directory.EnumerateDirectories("Jees_交易结算"))
{
    tasks.Add(generateResult(directory, Provider.Jees));
}

foreach (var directory in Directory.EnumerateDirectories("Rohon_交易核算单(盯市)"))
{
    tasks.Add(generateResult(directory, Provider.Rohon));
}

foreach (var directory in Directory.EnumerateDirectories("Shinny_交易结算单(盯市)"))
{
    tasks.Add(generateResult(directory, Provider.Shinny));
}

await Task.WhenAll(tasks);
