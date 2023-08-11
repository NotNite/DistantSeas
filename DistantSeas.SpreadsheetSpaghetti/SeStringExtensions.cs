using System.Text;
using Lumina.Text;
using Lumina.Text.Payloads;

namespace DistantSeas.SpreadsheetSpaghetti;

public static class SeStringExtensions {
    public static string TextValue(this SeString str) {
        return str.Payloads
                  .Where(x => x is TextPayload)
                  .Cast<TextPayload>()
                  .Aggregate(new StringBuilder(), (sb, tp) => sb.Append(
                                 Encoding.UTF8.GetString(tp.Data)
                             ), sb => sb.ToString());
    }
}
