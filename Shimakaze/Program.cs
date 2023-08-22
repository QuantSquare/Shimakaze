// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Text.RegularExpressions;

static Task GenerateResult(string directory, Providers provider)
{
    return Task.Run(() =>
    {
        foreach (var resultDir in Enum.GetNames<Results>())
        {
            _ = Directory.CreateDirectory($"Results{Path.DirectorySeparatorChar}{resultDir}{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}");
        }

        StringBuilder result = new();

        switch (provider)
        {
            case Providers.Jees:
                foreach (var file in Directory.EnumerateFiles(directory).Where(s => s.Split(Path.DirectorySeparatorChar)[3].StartsWith("结算单_")))
                {
                    Console.WriteLine($"Task={Task.CurrentId}, file={file}, Thread={Environment.CurrentManagedThreadId}");

                    foreach (var line in File.ReadLines(file).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号："))
                        {
                            if (line.Split(separatorArray0, StringSplitOptions.RemoveEmptyEntries)[0] != file.Split(Path.DirectorySeparatorChar)[2].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("日期："))
                        {
                            var date = line.Split(separatorArray1, StringSplitOptions.RemoveEmptyEntries)[0];
                            _ = result.Append($"\"{date}\"");
                            if (date != file.Split(Path.DirectorySeparatorChar)[3].Split(".")[0].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("交易日："))
                        {
                            _ = result.Append($",\"{line.Split(separatorArray2, StringSplitOptions.RemoveEmptyEntries)[0]}\"");
                        }
                        if (line.StartsWith("出入金Deposit/Withdrawal："))
                        {
                            var columns = line.Split(separatorArray3, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",\"{columns[0]}\",\"{columns[1]}\"");
                        }
                        if (line.StartsWith("权利金收入Premiumreceived："))
                        {
                            _ = result.Append($",\"{line.Split(separatorArray4, StringSplitOptions.RemoveEmptyEntries)[1]}\"");
                        }
                    }
                    _ = result.AppendLine();
                }
                break;
            case Providers.Lanyee:
                throw new NotImplementedException($"Providers={Providers.Lanyee}");
            case Providers.Rohon:
                foreach (var file in Directory.EnumerateFiles(directory).Where(s => s.Split(Path.DirectorySeparatorChar)[3].StartsWith("20")))
                {
                    Console.WriteLine($"Task={Task.CurrentId}, file={file}, Thread={Environment.CurrentManagedThreadId}");

                    string? date = null;
                    StringBuilder? fileResult = null;
                    StringBuilder positionClosed = new(), positions = new();
                    ParseTable? parseTable = null;
                    Dictionary<string, decimal> profitOrLoss = new(), riskExposure = new();

                    foreach (var line in File.ReadLines(file, Encoding.GetEncoding("GB18030")).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号："))
                        {
                            if (line.Split(separatorArray5, StringSplitOptions.RemoveEmptyEntries)[0] != file.Split(Path.DirectorySeparatorChar)[2].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("客户号ClientID:"))
                        {
                            if (line.Split(separatorArray6, StringSplitOptions.RemoveEmptyEntries)[0] != file.Split(Path.DirectorySeparatorChar)[2].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("日期："))
                        {
                            date = line.Split(separatorArray7, StringSplitOptions.RemoveEmptyEntries)[0];
                            _ = result.Append($"\"{date}\"");
                            if (date != file.Split(Path.DirectorySeparatorChar)[3].Split(".")[0].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("日期Date:"))
                        {
                            date = line.Split(separatorArray8, StringSplitOptions.RemoveEmptyEntries)[0];
                            _ = result.Append($"\"{date}\"");
                            if (date != file.Split(Path.DirectorySeparatorChar)[3].Split(".")[0].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("出入金："))
                        {
                            var columns = line.Split(separatorArray9, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",\"{columns[0]}\",\"{columns[1]}\"");
                        }
                        if (line.StartsWith("出入金Deposit/Withdrawal:"))
                        {
                            var columns = line.Split(separatorArray10, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",\"{columns[0]}\",\"{columns[1]}\"");
                        }
                        if (line.StartsWith("平仓盈亏："))
                        {
                            _ = result.Append($",\"{line.Split(separatorArray11, StringSplitOptions.RemoveEmptyEntries)[2]}\"");
                        }
                        if (line.StartsWith("权利金收入premiumreceived:"))
                        {
                            _ = result.Append($",\"{line.Split(separatorArray12, StringSplitOptions.RemoveEmptyEntries)[1]}\"");
                        }
                        if (line.StartsWith("|") && !line.StartsWith("|共"))
                        {
                            switch (line)
                            {
                                case string when line.StartsWith("|发生日期|出入金类型|入金|出金|说明|"):
                                case string when line.StartsWith("|Date|Type|Deposit|Withdrawal|Note|"):
                                case string when line.StartsWith("|成交日期|交易所|品种|交割期|买卖|投保|成交价|手数|成交额|开平|手续费|平仓盈亏|成交序号|成交时间|"):
                                case string when line.StartsWith("|成交日期|交易所|品种|合约|买/卖|投/保|成交价|手数|成交额|开平|手续费|平仓盈亏|权利金收支|成交序号|成交类型|"):
                                case string when line.StartsWith("|Date|Exchange|Product|Instrument|B/S|S/H|Price|Lots|Turnover|O/C|Fee|RealizedP/L|PremiumReceived/Paid|Trans.No.|TradeType|"):
                                case string when line.StartsWith("|交割日期|交易所|品种|合约|投/保|买/卖|是否行权|行权数量|行权价格|行权金额|行权盈亏|行权手续费|"):
                                case string when line.StartsWith("|Date|Exchange|Product|Instrument|S/H|B/S|Exercise/Abandon|VolumeExercised|ExercisePrice|AmountExercised|ExerciseP/L|ExerciseFee|"):
                                case string when line.StartsWith("|交割日期|交易所|品种|交割期|投/保|买/卖|实提数量|剩余数量|交割价格|交割金额|交割手续费|开仓均价|交割盈亏|"):
                                case string when line.StartsWith("|Date|Exchange|Product|Del.Mth|S/H|B/S|DeliveredQty|Residual|DeliveryPrice|DeliveryAmount|DeliveryFee|AvgOpeningPrice|RealizedP/LatDelivery|"):
                                case string when line.StartsWith("|交易所|合约代码|交割期|开仓日期|投/保|买/卖|持仓量|开仓价|昨结算价|今结算价|浮动盈亏|盯市盈亏|保证金|"):
                                case string when line.StartsWith("|交易所|品种|合约|开仓日期|投/保|买/卖|持仓量|开仓价|昨结算|结算价|浮动盈亏|盯市盈亏|保证金|期权市值|"):
                                case string when line.StartsWith("|Exchange|Product|Instrument|OpenDate|S/H|B/S|Positon|Pos.OpenPrice|Prev.Sttl|SettlementPrice|Accum.P/L|MTMP/L|Margin|MarketValue(Options)|"):
                                case string when line.StartsWith("|平仓日期|交易所|品种|合约|开仓日期|买/卖|手数|开仓价|昨结算|成交价|平仓盈亏|权利金收支|"):
                                case string when line.StartsWith("|品种|合约|买持|买均价|卖持|卖均价|昨结算|今结算|持仓盯市盈亏|保证金占用|投保|多头期权市值|空头期权市值|"):
                                    fileResult = null;
                                    parseTable = null;
                                    break;
                                case string when line.StartsWith("|平仓日期|交易所|品种|交割期|开仓日期|买/卖|手数|开仓价|昨核算|成交价|平仓盈亏|"):
                                case string when line.StartsWith("|CloseDate|Exchange|Product|Instrument|OpenDate|B/S|Lots|Pos.OpenPrice|Prev.Sttl|Trans.Price|RealizedP/L|PremiumReceived/Paid|"):
                                    fileResult = positionClosed;
                                    parseTable = ParsePositionClosed;
                                    break;
                                case string when line.StartsWith("|合约代码|交割期|买持|买均价|卖持|卖均价|昨结算价|今结算价|持仓盯市盈亏|保证金占用|投/保|"):
                                case string when line.StartsWith("|Product|Instrument|LongPos.|AvgBuyPrice|ShortPos.|AvgSellPrice|Prev.Sttl|SttlToday|MTMP/L|MarginOccupied|S/H|MarketValue(Long)|MarketValue(Short)|"):
                                    fileResult = positions;
                                    parseTable = ParsePositions;
                                    break;
                                default:
                                    if (fileResult != null && parseTable != null)
                                    {
                                        parseTable.Invoke(line.Split("|"), fileResult, profitOrLoss, riskExposure, provider);
                                    }
                                    break;
                            }
                        }
                    }

                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}PositionClosed{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}PositionClosed_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", new[] { positionClosed.ToString() }, Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}Positions{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}Positions_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", new[] { positions.ToString() }, Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}ProfitOrLoss{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}ProfitOrLoss_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", profitOrLoss.OrderByDescending(s => s.Value).Select(s => $"\"{s.Key}\",\"{s.Value}\""), Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}RiskExposure{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}RiskExposure_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", riskExposure.OrderByDescending(s => s.Value).Select(s => $"\"{s.Key}\",\"{s.Value}\""), Encoding.UTF8);

                    _ = result.AppendLine();
                }
                break;
            case Providers.Shinny:
                foreach (var file in Directory.EnumerateFiles(directory).Where(s => s.Split(Path.DirectorySeparatorChar)[3].StartsWith("结算单_")))
                {
                    Console.WriteLine($"Task={Task.CurrentId}, file={file}, Thread={Environment.CurrentManagedThreadId}");

                    string? date = null;
                    StringBuilder? fileResult = null;
                    StringBuilder positionClosed = new(), positions = new();
                    ParseTable? parseTable = null;
                    Dictionary<string, decimal> profitOrLoss = new(), riskExposure = new();

                    foreach (var line in File.ReadAllLines(file, Encoding.GetEncoding("GB18030")).Select(s => s.Replace(" ", "")))
                    {
                        if (line.StartsWith("客户号ClientID："))
                        {
                            if (line.Split(separatorArray13, StringSplitOptions.RemoveEmptyEntries)[0] != file.Split(Path.DirectorySeparatorChar)[2].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("日期Date："))
                        {
                            date = line.Split(separatorArray14, StringSplitOptions.RemoveEmptyEntries)[0];
                            _ = result.Append($"\"{date}\"");
                            if (date != file.Split(Path.DirectorySeparatorChar)[3].Split(".")[0].Split("_")[^1])
                            {
                                throw new InvalidOperationException($"File={file}");
                            }
                        }
                        if (line.StartsWith("出入金Deposit/Withdrawal："))
                        {
                            var columns = line.Split(separatorArray15, StringSplitOptions.RemoveEmptyEntries);
                            _ = result.Append($",\"{columns[0]}\",\"{columns[1]}\"");
                        }
                        if (line.StartsWith("权利金收入Premiumreceived："))
                        {
                            _ = result.Append($",\"{line.Split(separatorArray16, StringSplitOptions.RemoveEmptyEntries)[1]}\"");
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
                                        parseTable.Invoke(line.Split("|"), fileResult, profitOrLoss, riskExposure, provider);
                                    }
                                    break;
                            }
                        }
                    }

                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}PositionClosed{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}PositionClosed_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", new[] { positionClosed.ToString() }, Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}Positions{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}Positions_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", new[] { positions.ToString() }, Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}ProfitOrLoss{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}ProfitOrLoss_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", profitOrLoss.OrderByDescending(s => s.Value).Select(s => $"\"{s.Key}\",\"{s.Value}\""), Encoding.UTF8);
                    File.WriteAllLines($"Results{Path.DirectorySeparatorChar}RiskExposure{Path.DirectorySeparatorChar}{directory.Split(Path.DirectorySeparatorChar)[2]}{Path.DirectorySeparatorChar}RiskExposure_{directory.Split(Path.DirectorySeparatorChar)[2]}_{date}.csv", riskExposure.OrderByDescending(s => s.Value).Select(s => $"\"{s.Key}\",\"{s.Value}\""), Encoding.UTF8);

                    _ = result.AppendLine();
                }
                break;
            default:
                throw new NotImplementedException("Providers=Unknown");
        }

        File.WriteAllLines($"Results{Path.DirectorySeparatorChar}Result_{directory.Split(Path.DirectorySeparatorChar)[2]}.csv", new[] { result.ToString() }, Encoding.UTF8);
    });
}

static void ParsePositionClosed(string[] columns, StringBuilder fileResult, Dictionary<string, decimal> profitOrLoss, Dictionary<string, decimal> riskExposure, Providers provider)
{
    string? product = null;

    switch (provider)
    {
        case Providers.Jees:
            throw new NotImplementedException($"Providers={Providers.Jees}");
        case Providers.Lanyee:
            throw new NotImplementedException($"Providers={Providers.Lanyee}");
        case Providers.Rohon:
            _ = fileResult.AppendLine($"\"{columns[3]}\",\"{columns[4]}\",\"{columns[6]}\",\"{columns[7]}\",\"{columns[11]}\"");
            product = InstrumentToProductRegex().Replace(columns[3], "");
            if (!profitOrLoss.ContainsKey(product))
            {
                profitOrLoss.Add(product, decimal.Parse(columns[11]));
            }
            else
            {
                profitOrLoss[product] += decimal.Parse(columns[11]);
            }
            break;
        case Providers.Shinny:
            _ = fileResult.AppendLine($"\"{columns[6]}\",\"{columns[9]}\",\"{columns[10]}\",\"{columns[14]}\"");
            product = InstrumentToProductRegex().Replace(columns[6], "");
            if (!profitOrLoss.ContainsKey(product))
            {
                profitOrLoss.Add(product, decimal.Parse(columns[14]));
            }
            else
            {
                profitOrLoss[product] += decimal.Parse(columns[14]);
            }
            break;
        default:
            throw new NotImplementedException("Providers=Unknown");
    }
}

static void ParsePositions(string[] columns, StringBuilder fileResult, Dictionary<string, decimal> profitOrLoss, Dictionary<string, decimal> riskExposure, Providers provider)
{
    string? product = null;

    switch (provider)
    {
        case Providers.Jees:
            throw new NotImplementedException($"Providers={Providers.Jees}");
        case Providers.Lanyee:
            throw new NotImplementedException($"Providers={Providers.Lanyee}");
        case Providers.Rohon:
            _ = fileResult.AppendLine($"\"{columns[1]}\",\"{columns[2]}\",\"{columns[3]}\",\"{columns[4]}\",\"{columns[5]}\",\"{columns[6]}\",\"{columns[8]}\",\"{columns[9]}\"");

            product = InstrumentToProductRegex().Replace(columns[1], "");

            if (!profitOrLoss.ContainsKey(product))
            {
                profitOrLoss.Add(product, decimal.Parse(columns[9]));
            }
            else
            {
                profitOrLoss[product] += decimal.Parse(columns[9]);
            }

            if (product is not "TL" and not "T" and not "TF" and not "TS")
            {
                if (!riskExposure.ContainsKey(product))
                {
                    riskExposure.Add(product, ((decimal.Parse(columns[3]) * decimal.Parse(columns[4])) - (decimal.Parse(columns[5]) * decimal.Parse(columns[6]))) * tradingUnit[product.ToUpper()]);
                }
                else
                {
                    riskExposure[product] += ((decimal.Parse(columns[3]) * decimal.Parse(columns[4])) - (decimal.Parse(columns[5]) * decimal.Parse(columns[6]))) * tradingUnit[product.ToUpper()];
                }
            }
            break;
        case Providers.Shinny:
            _ = fileResult.AppendLine($"\"{columns[4]}\",\"{columns[5]}\",\"{columns[6]}\",\"{columns[7]}\",\"{columns[8]}\",\"{columns[10]}\",\"{columns[11]}\"");

            product = InstrumentToProductRegex().Replace(columns[4], "");

            if (!profitOrLoss.ContainsKey(product))
            {
                profitOrLoss.Add(product, decimal.Parse(columns[11]));
            }
            else
            {
                profitOrLoss[product] += decimal.Parse(columns[11]);
            }

            if (product is not "TL" and not "T" and not "TF" and not "TS")
            {
                if (!riskExposure.ContainsKey(product))
                {
                    riskExposure.Add(product, ((decimal.Parse(columns[5]) * decimal.Parse(columns[6])) - (decimal.Parse(columns[7]) * decimal.Parse(columns[8]))) * tradingUnit[product.ToUpper()]);
                }
                else
                {
                    riskExposure[product] += ((decimal.Parse(columns[5]) * decimal.Parse(columns[6])) - (decimal.Parse(columns[7]) * decimal.Parse(columns[8]))) * tradingUnit[product.ToUpper()];
                }
            }
            break;
        default:
            throw new NotImplementedException("Providers=Unknown");
    }
}

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

_ = Directory.CreateDirectory("Results");

foreach (var result in Enum.GetNames<Results>())
{
    _ = Directory.CreateDirectory($"Results{Path.DirectorySeparatorChar}{result}");
}

List<Task>? tasks = new();

foreach (var provider in Enum.GetNames<Providers>())
{
    var providerEnum = Enum.Parse<Providers>(provider);

    if (providerEnum != Providers.Lanyee)
    {
        foreach (var directory in Directory.EnumerateDirectories(@".\", $"{provider}_*"))
        {
            foreach (var subdirectory in Directory.EnumerateDirectories(directory))
            {
                tasks.Add(GenerateResult(subdirectory, providerEnum));
            }
        }
    }
}

await Task.WhenAll(tasks);

internal enum Providers
{
    Jees,
    Lanyee,
    Rohon,
    Shinny
}

internal enum Results
{
    PositionClosed,
    Positions,
    ProfitOrLoss,
    RiskExposure
}

internal delegate void ParseTable(string[] columns, StringBuilder fileResult, Dictionary<string, decimal> profitOrLoss, Dictionary<string, decimal> riskExposure, Providers provider);

internal partial class Program
{
    [GeneratedRegex("[0-9]")]
    private static partial Regex InstrumentToProductRegex();

    private static Dictionary<string, decimal> tradingUnit = new()
    {
        { "A", 10 },
        { "AG", 15 },
        { "AL", 5 },
        { "AP", 10 },
        { "AU", 1000 },
        { "BU", 10 },
        { "C", 10 },
        { "CF", 5 },
        { "CS", 10 },
        { "CU", 5 },
        { "FG", 20 },
        { "FU", 10 },
        { "HC", 10 },
        { "I", 100 },
        { "IC", 200 },
        { "IF", 300 },
        { "IH", 300 },
        { "IM", 200 },
        { "J", 100 },
        { "JD", 10 },
        { "JM", 60 },
        { "L", 5 },
        { "M", 10 },
        { "MA", 10 },
        { "NI", 1 },
        { "OI", 10 },
        { "P", 10 },
        { "PB", 5 },
        { "PK", 5 },
        { "PP", 5 },
        { "RB", 10 },
        { "RM", 10 },
        { "RU", 10 },
        { "SF", 5 },
        { "SM", 5 },
        { "SN", 1 },
        { "SR", 10 },
        { "T", 1000000 },
        { "TA", 5 },
        { "TF", 1000000 },
        { "TS", 2000000 },
        { "TL", 1000000 },
        { "V", 5 },
        { "Y", 10 },
        { "ZC", 100 },
        { "ZN", 5 }
    };

    private static readonly string[] separatorArray0 = new[] { "客户号：" };
    private static readonly string[] separatorArray1 = new[] { "日期：" };
    private static readonly string[] separatorArray2 = new[] { "交易日：" };
    private static readonly string[] separatorArray3 = new[] { "出入金Deposit/Withdrawal：", "期末结存Balancec/f：" };
    private static readonly string[] separatorArray4 = new[] { "权利金收入Premiumreceived：", "风险度RiskDegree：" };
    private static readonly string[] separatorArray5 = new[] { "客户号：", "客户名称：" };
    private static readonly string[] separatorArray6 = new[] { "客户号ClientID:", "客户名称ClientName:" };
    private static readonly string[] separatorArray7 = new[] { "日期：" };
    private static readonly string[] separatorArray8 = new[] { "日期Date:" };
    private static readonly string[] separatorArray9 = new[] { "出入金：", "期末结存：", "可用资金：" };
    private static readonly string[] separatorArray10 = new[] { "出入金Deposit/Withdrawal:", "期末结存Balancec/f:" };
    private static readonly string[] separatorArray11 = new[] { "平仓盈亏：", "质押金：", "风险度：" };
    private static readonly string[] separatorArray12 = new[] { "权利金收入premiumreceived:", "风险度RiskDegree:" };
    private static readonly string[] separatorArray13 = new[] { "客户号ClientID：", "客户名称ClientName：" };
    private static readonly string[] separatorArray14 = new[] { "日期Date：" };
    private static readonly string[] separatorArray15 = new[] { "出入金Deposit/Withdrawal：", "期末结存Balancec/f：" };
    private static readonly string[] separatorArray16 = new[] { "权利金收入Premiumreceived：", "风险度RiskDegree：" };
}
