// See https://aka.ms/new-console-template for more information
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

static Task generateResult(string directory, Provider provider)
{
    return Task.Run(() =>
    {
        Directory.CreateDirectory($"Results{Path.DirectorySeparatorChar}PositionClosed{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[1]}");
        Directory.CreateDirectory($"Results{Path.DirectorySeparatorChar}Positions{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[1]}");

        StringBuilder result = new();

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
                            _ = result.Append($"\"{clientId}\"");
                        }
                        if (line.StartsWith("日期："))
                        {
                            _ = result.Append($",\"{line.Split(new[] { "日期：" }, StringSplitOptions.RemoveEmptyEntries)[0]}\"");
                        }
                        if (line.StartsWith("交易日："))
                        {
                            _ = result.Append($",\"{line.Split(new[] { "交易日：" }, StringSplitOptions.RemoveEmptyEntries)[0]}\"");
                        }
                        if (line.StartsWith("出入金Deposit/Withdrawal："))
                        {
                            var columns = line.Split(new[] { "出入金Deposit/Withdrawal：", "期末结存Balancec/f：" }, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",\"{columns[0]}\",\"{columns[1]}\"");
                        }
                        if (line.StartsWith("权利金收入Premiumreceived："))
                        {
                            _ = result.Append($",\"{line.Split(new[] { "权利金收入Premiumreceived：", "风险度RiskDegree：" }, StringSplitOptions.RemoveEmptyEntries)[1]}\"");
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

                    string? date = null;
                    StringBuilder? fileResult = null;
                    StringBuilder positionClosed = new(), positions = new();
                    ParseTable? parseTable = null;

                    foreach (var line in File.ReadLines(file, Encoding.GetEncoding("GB18030")).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号："))
                        {
                            var clientId = line.Split(new[] { "客户号：", "客户名称：" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (clientId != file.Split(Path.DirectorySeparatorChar)[1].Split("_")[^1])
                            {
                                throw new InvalidOperationException();
                            }
                            _ = result.Append($"\"{clientId}\"");
                        }
                        if (line.StartsWith("日期："))
                        {
                            date = line.Split(new[] { "日期：" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            _ = result.Append($",\"{date}\"");
                        }
                        if (line.StartsWith("出入金："))
                        {
                            var columns = line.Split(new[] { "出入金：", "期末结存：", "可用资金：" }, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",\"{columns[0]}\",\"{columns[1]}\"");
                        }
                        if (line.StartsWith("平仓盈亏："))
                        {
                            _ = result.Append($",\"{line.Split(new[] { "平仓盈亏：", "质押金：", "风险度：" }, StringSplitOptions.RemoveEmptyEntries)[2]}\"");
                        }
                        if (line.StartsWith("|") && !line.StartsWith("|共"))
                        {
                            switch (line)
                            {
                                case string when line.StartsWith("|成交日期|交易所|品种|交割期|买卖|投保|成交价|手数|成交额|开平|手续费|平仓盈亏|成交序号|成交时间|"):
                                case string when line.StartsWith("|交割日期|交易所|品种|合约|投/保|买/卖|是否行权|行权数量|行权价格|行权金额|行权盈亏|行权手续费|"):
                                case string when line.StartsWith("|交易所|合约代码|交割期|开仓日期|投/保|买/卖|持仓量|开仓价|昨结算价|今结算价|浮动盈亏|盯市盈亏|保证金|"):
                                    fileResult = null;
                                    parseTable = null;
                                    break;
                                case string when line.StartsWith("|平仓日期|交易所|品种|交割期|开仓日期|买/卖|手数|开仓价|昨核算|成交价|平仓盈亏|"):
                                    fileResult = positionClosed;
                                    parseTable = ParsePositionClosed;
                                    break;
                                case string when line.StartsWith("|合约代码|交割期|买持|买均价|卖持|卖均价|昨结算价|今结算价|持仓盯市盈亏|保证金占用|投/保|"):
                                    fileResult = positions;
                                    parseTable = ParsePositions;
                                    break;
                                default:
                                    if (fileResult != null && parseTable != null)
                                    {
                                        parseTable.Invoke(line.Split("|"), fileResult, provider);
                                    }
                                    break;
                            }
                        }
                    }

                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}PositionClosed{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[1]}{Path.DirectorySeparatorChar}PositionClosed_{directory.Split(Path.DirectorySeparatorChar)[1]}_{date}.csv", new[] { positionClosed.ToString() }, Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}Positions{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[1]}{Path.DirectorySeparatorChar}Positions_{directory.Split(Path.DirectorySeparatorChar)[1]}_{date}.csv", new[] { positions.ToString() }, Encoding.UTF8);

                    _ = result.AppendLine();
                }
                break;
            case Provider.Shinny:
                foreach (var file in Directory.EnumerateFiles(directory).Where(s => s.Split(Path.DirectorySeparatorChar)[2].StartsWith("结算单_")))
                {
                    Console.WriteLine($"Task={Task.CurrentId}, file={file}, Thread={Environment.CurrentManagedThreadId}");

                    string? date = null;
                    StringBuilder? fileResult = null;
                    StringBuilder positionClosed = new(), positions = new();
                    ParseTable? parseTable = null;

                    foreach (var line in File.ReadAllLines(file, Encoding.GetEncoding("GB18030")).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号ClientID："))
                        {
                            var clientId = line.Split(new[] { "客户号ClientID：", "客户名称ClientName：" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (clientId != file.Split(Path.DirectorySeparatorChar)[1].Split("_")[^1])
                            {
                                throw new InvalidOperationException();
                            }
                            _ = result.Append($"\"{clientId}\"");
                        }
                        if (line.StartsWith("日期Date："))
                        {
                            date = line.Split(new[] { "日期Date：" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            _ = result.Append($",\"{date}\"");
                        }
                        if (line.StartsWith("出入金Deposit/Withdrawal："))
                        {
                            var columns = line.Split(new[] { "出入金Deposit/Withdrawal：", "期末结存Balancec/f：" }, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",\"{columns[0]}\",\"{columns[1]}\"");
                        }
                        if (line.StartsWith("权利金收入Premiumreceived："))
                        {
                            _ = result.Append($",\"{line.Split(new[] { "权利金收入Premiumreceived：", "风险度RiskDegree：" }, StringSplitOptions.RemoveEmptyEntries)[1]}\"");
                        }
                        if (line.StartsWith("|") && !line.StartsWith("|共"))
                        {
                            switch (line)
                            {
                                case string when line.StartsWith("|发生日期|出入金类型|入金|出金|汇率|资金账号|说明|"):
                                case string when line.StartsWith("|Date|Type|Deposit|Withdrawal|ExchangeRate|AccountID|Note|"):
                                case string when line.StartsWith("|成交日期|投资单元|交易所|交易编码|品种|合约|买/卖|投/保|成交价|手数|成交额|开平|手续费|平仓盈亏|权利金收支|成交序号|资金账号|"):
                                case string when line.StartsWith("|Date|InvestUnit|Exchange|tradingcode|Product|Instrument|B/S|S/H|Price|Lots|Turnover|O/C|Fee|RealizedP/L|PremiumReceived/Paid|Trans.No.|AccountID|"):
                                case string when line.StartsWith("|平仓日期|投资单元|交易所|交易编码|品种|合约|开仓日期|投/保|买/卖|手数|开仓价|昨结算|成交价|平仓盈亏|权利金收支|资金账号|"):
                                case string when line.StartsWith("|投资单元|交易所|交易编码|品种|合约|开仓日期|投/保|买/卖|持仓量|开仓价|昨结算|结算价|浮动盈亏|盯市盈亏|保证金|期权市值|资金账号|"):
                                case string when line.StartsWith("|InvestUnit|Exchange|tradingcode|Product|Instrument|OpenDate|S/H|B/S|Positon|Pos.OpenPrice|Prev.Sttl|SettlementPrice|Accum.P/L|MTMP/L|Margin|MarketValue(Options)|AccountID|"):
                                case string when line.StartsWith("|投资单元|交易编码|品种|合约|买持|买均价|卖持|卖均价|昨结算|今结算|持仓盯市盈亏|保证金占用|投/保|多头期权市值|空头期权市值|资金账号|"):
                                    fileResult = null;
                                    parseTable = null;
                                    break;
                                case string when line.StartsWith("|CloseDate|InvestUnit|Exchange|tradingcode|Product|Instrument|OpenDate|S/H|B/S|Lots|Pos.OpenPrice|Prev.Sttl|Trans.Price|RealizedP/L|PremiumReceived/Paid|AccountID|"):
                                    fileResult = positionClosed;
                                    parseTable = ParsePositionClosed;
                                    break;
                                case string when line.StartsWith("|InvestUnit|tradingcode|Product|Instrument|LongPos.|AvgBuyPrice|ShortPos.|AvgSellPrice|Prev.Sttl|SttlToday|MTMP/L|MarginOccupied|S/H|MarketValue(Long)|MarketValue(Short)|AccountID|"):
                                    fileResult = positions;
                                    parseTable = ParsePositions;
                                    break;
                                default:
                                    if (fileResult != null && parseTable != null)
                                    {
                                        parseTable.Invoke(line.Split("|"), fileResult, provider);
                                    }
                                    break;
                            }
                        }
                    }

                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}PositionClosed{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[1]}{Path.DirectorySeparatorChar}PositionClosed_{directory.Split(Path.DirectorySeparatorChar)[1]}_{date}.csv", new[] { positionClosed.ToString() }, Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}Positions{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[1]}{Path.DirectorySeparatorChar}Positions_{directory.Split(Path.DirectorySeparatorChar)[1]}_{date}.csv", new[] { positions.ToString() }, Encoding.UTF8);

                    _ = result.AppendLine();
                }
                break;
            default:
                throw new NotImplementedException();
        }

        File.WriteAllLines($"Results{Path.DirectorySeparatorChar}Result_{directory.Split(Path.DirectorySeparatorChar)[1]}.csv", new[] { result.ToString() }, Encoding.UTF8);
    });
}

static void ParsePositionClosed(string[] columns, StringBuilder fileResult, Provider provider)
{
    switch (provider)
    {
        case Provider.Jees:
            throw new NotImplementedException();
        case Provider.Lanyee:
            throw new NotImplementedException();
        case Provider.Rohon:
            fileResult.AppendLine($"\"{columns[3]}\",\"{columns[6]}\",\"{columns[7]}\",\"{columns[11]}\"");
            break;
        case Provider.Shinny:
            fileResult.AppendLine($"\"{columns[6]}\",\"{columns[9]}\",\"{columns[10]}\",\"{columns[14]}\"");
            break;
        default:
            throw new NotImplementedException();
    }
}

static void ParsePositions(string[] columns, StringBuilder fileResult, Provider provider)
{
    switch (provider)
    {
        case Provider.Jees:
            throw new NotImplementedException();
        case Provider.Lanyee:
            throw new NotImplementedException();
        case Provider.Rohon:
            fileResult.AppendLine($"\"{columns[1]}\",\"{columns[3]}\",\"{columns[5]}\",\"{columns[8]}\",\"{columns[9]}\"");
            break;
        case Provider.Shinny:
            fileResult.AppendLine($"\"{columns[4]}\",\"{columns[5]}\",\"{columns[7]}\",\"{columns[10]}\",\"{columns[11]}\"");
            break;
        default:
            throw new NotImplementedException();
    }
}

Directory.CreateDirectory("Results");
Directory.CreateDirectory($"Results{Path.DirectorySeparatorChar}PositionClosed");
Directory.CreateDirectory($"Results{Path.DirectorySeparatorChar}Positions");

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

internal enum Provider
{
    Jees,
    Lanyee,
    Rohon,
    Shinny,
}

internal delegate void ParseTable(string[] columns, StringBuilder fileResult, Provider provider);
