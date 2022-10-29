using System.Security.Cryptography;
using System.Text;

namespace Wilgysef.Stalk.TestBase;

public static class RandomValues
{
    public static char[] RandomStringDefaultCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public static int RandomInt(int toExclusive)
    {
        return RandomNumberGenerator.GetInt32(toExclusive);
    }

    public static int RandomInt(int fromInclusive, int toExclusive)
    {
        return RandomNumberGenerator.GetInt32(fromInclusive, toExclusive);
    }

    public static long RandomLong()
    {
        return Math.Abs(BitConverter.ToInt64(RandomNumberGenerator.GetBytes(sizeof(long))));
    }

    public static long RandomLong(long toExclusive)
    {
        return RandomLong() % toExclusive;
    }

    public static long RandomLong(long fromExclusive, long toExclusive)
    {
        return RandomLong() % (toExclusive - fromExclusive) + fromExclusive;
    }

    public static double RandomDouble()
    {
        return (BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong))) >> 11) * (1.0 / (1ul << 53));
    }

    public static double RandomDouble(double min, double max)
    {
        return RandomDouble() * (max - min) + min;
    }

    public static string RandomString(int length)
    {
        return RandomString(length, RandomStringDefaultCharset);
    }

    public static string RandomString(int length, char[] charset)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            builder.Append(charset[RandomInt(charset.Length)]);
        }

        return builder.ToString();
    }

    public static DateTime RandomDateTime(DateTime startTime, DateTime endTime, DateTimePrecision precision = DateTimePrecision.Seconds)
    {
        return precision switch
        {
            DateTimePrecision.Seconds => startTime.AddSeconds(RandomDouble(0, (endTime - startTime).TotalSeconds)),
            DateTimePrecision.Minutes => startTime.AddMinutes(RandomDouble(0, (endTime - startTime).TotalMinutes)),
            DateTimePrecision.Hours => startTime.AddHours(RandomDouble(0, (endTime - startTime).TotalHours)),
            DateTimePrecision.Days => startTime.AddDays(RandomDouble(0, (endTime - startTime).TotalDays)),
            _ => throw new ArgumentOutOfRangeException(nameof(precision)),
        };
    }

    public static DateTime RandomDateTime()
    {
        var curTime = DateTime.Now;
        return RandomDateTime(curTime.Date, curTime.Date.AddYears(1));
    }

    public static T[] EnumValues<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>();
    }

    public static T RandomEnum<T>() where T : struct, Enum
    {
        var values = EnumValues<T>();
        return values[RandomInt(values.Length)];
    }

    public static long RandomJobId()
    {
        return RandomLong(1, 1_000_000_000L);
    }

    public static long RandomJobTaskId()
    {
        return RandomLong(1, 1_000_000_000L);
    }

    public static string RandomDirPath(int depth, char separator = '/')
    {
        var builder = new StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            builder.Append(RandomString(RandomInt(4, 16)));
            builder.Append(separator);
        }
        return builder.ToString();
    }

    public static string RandomFilePath(int depth, char separator = '/')
    {
        var path = RandomDirPath(depth - 1);
        return path + separator + RandomString(RandomInt(10, 20)) + "." + RandomString(3);
    }

    public static Uri RandomUri(
        string? scheme = "https",
        string? host = null,
        int port = 443,
        string? path = null,
        string? query = null,
        string? fragment = null)
    {
        var builder = new UriBuilder(
            scheme,
            host ?? (RandomString(8) + ".com"),
            port,
            path ?? RandomString(16))
        {
            Query = query,
            Fragment = fragment
        };
        return builder.Uri;
    }

    public enum DateTimePrecision
    {
        Seconds,
        Minutes,
        Hours,
        Days,
    }
}
