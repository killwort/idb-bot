using System.Threading;
using System.Threading.Tasks;
using IBDTools.Screens;

namespace IBDTools {
    public static class Extensions {
        public static async Task Activation(this IScreen screen, CancellationToken cancellationToken) {
            while (!screen.IsScreenActive()) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(250, cancellationToken);
            }
        }
        public static string Pretty(this long n) {
            var result = "";
            if (n < 0) {
                n = -n;
                result += "-";
            }

            if (n > 1_000_000_000)
                result += ((double) n / 1_000_000_000).ToString("0.##") + "B";
            else if (n > 1_000_000)
                result += ((double) n / 1_000_000).ToString("0.##") + "M";
            else if (n > 1_000)
                result += ((double) n / 1_000).ToString("0.##") + "K";
            else result += n.ToString();
            return result;
        }
    }
}