using RivalCoins.Sdk;
using stellar_dotnet_sdk;

namespace RivalCoins.Api;

public static class Program
{
    private static async Task RivalCoinCirculatingSupplyServerAsync(RivalCoins.Sdk.Network network, IProgress<List<CirculatingSupply>> progress)
    {
        var NonRivalCoinAssetCodes = new[] { "MONEY", "PlayMONEY", "USA", "PlayUSA" };
        var _rivalCoinDistributionAccountId = "GAREA377PBB4AOZD4CD6SNCVDNOBC3JZZ6IOAXEXNEE2QGQX5JQTC24Z";
        var maxBalanceSignificantDigits = ChangeTrustOperation.MaxLimit.Length - 1;
        var MaxDecimalPrecision = 7;

        var wallet = Wallet.Default[network] with { HomeDomain = "Value Does Not Matter" };

        await wallet.InitializeAsync(_rivalCoinDistributionAccountId);

        while (true)
        {
            var newCirculatingSupplies = new List<CirculatingSupply>();
            var rivalCoinAssets = await Wallet.GetRivalCoinsAsync(wallet.Network);
            var rivalCoins = rivalCoinAssets.Where(rivalCoin => !NonRivalCoinAssetCodes.Contains(rivalCoin.Asset.Code)).ToList();
            var nonRivalCoins = rivalCoinAssets.Where(rivalCoin => NonRivalCoinAssetCodes.Contains(rivalCoin.Asset.Code)).ToList();

            var rivalCoinDistributionAccount = await wallet.Server.Accounts.Account(_rivalCoinDistributionAccountId);

            // get non-Rival Coins
            foreach (var nonRivalCoin in nonRivalCoins)
            {
                var nonRivalCoinAssetInfo = await wallet.Server.Assets.AssetCode(nonRivalCoin.Asset.Code).AssetIssuer(nonRivalCoin.Asset.Issuer).Execute();

                newCirculatingSupplies.Add(new CirculatingSupply(nonRivalCoin.Asset, double.Parse(nonRivalCoinAssetInfo.Records.First().Amount)));
            }

            // get Rival Coins
            foreach (var rivalCoin in rivalCoins)
            {
                var rivalCoinBalance = rivalCoinDistributionAccount.Balances.FirstOrDefault(balance => balance.AssetCode == rivalCoin.Asset.Code && balance.AssetIssuer == rivalCoin.Asset.Issuer);
                if (rivalCoinBalance != null)
                {
                    var buyingLiabilities = double.Parse(rivalCoinBalance.BuyingLiabilities);
                    var sellingLiabilities = double.Parse(rivalCoinBalance.SellingLiabilities);
                    var buying = long.Parse(buyingLiabilities.ToString($"F{MaxDecimalPrecision}").Replace(".", string.Empty));
                    var selling = long.Parse(sellingLiabilities.ToString($"F{MaxDecimalPrecision}").Replace(".", string.Empty));
                    var diff = buying - selling;
                    var circulatingSupply = double.Parse(diff.ToString($"D{maxBalanceSignificantDigits}").Insert(maxBalanceSignificantDigits - MaxDecimalPrecision, "."));

                    newCirculatingSupplies.Add(new CirculatingSupply(rivalCoin.Asset, circulatingSupply));
                }
            }

            progress.Report(newCirculatingSupplies);

            await Task.Delay(60 * 1 * 1000);
        }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        var network = RivalCoins.Sdk.Network.Mainnet;
        var circulatingSupplies = new List<CirculatingSupply>();
        var circulatingSupplyRefresh = new Progress<List<CirculatingSupply>>();

        circulatingSupplyRefresh.ProgressChanged += (s, currentCirculatingSupplies) => {
            Console.WriteLine("updating circulating supply");
            circulatingSupplies = currentCirculatingSupplies;
        };

        _ = RivalCoinCirculatingSupplyServerAsync(network, circulatingSupplyRefresh);

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapGet("/rivalcoincirculatingsupplies", () =>
        {
            return circulatingSupplies;
        })
        .WithName("GetRivalCoinCirculatingSupplies");

        app.Run();
    }

    internal record CirculatingSupply(AssetTypeCreditAlphaNum Asset, double Value);
}
