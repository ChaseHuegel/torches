using System.Text;

namespace Library.Util;

public static class StringUtils
{
    public static string JoinValid(char separator, params string?[] values)
    {
        var builder = new StringBuilder();

        for (int i = 0; i < values.Length; i++)
        {
            string? value = values[i];
            if (!string.IsNullOrEmpty(value))
            {
                if (builder.Length > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(value);
            }
        }

        return builder.ToString();
    }
}