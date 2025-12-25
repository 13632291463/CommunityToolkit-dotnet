// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Common;

/// <summary>
/// 字符串和字符串表示操作的辅助类
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 匹配电话号码的正则表达式
    /// </summary>
    internal const string PhoneNumberRegex = @"^[+]?(\d{1,3})?[\s.-]?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$";

    /// <summary>
    /// 匹配仅包含字母的字符串的正则表达式
    /// </summary>
    internal const string CharactersRegex = "^[A-Za-z]+$";

    /// <summary>
    /// 匹配电子邮件地址的正则表达式
    /// </summary>
    /// <remarks>通用电子邮件正则表达式（RFC 5322 官方标准），来自 https://emailregex.com</remarks>
    internal const string EmailRegex = "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])";

    /// <summary>
    /// 要移除的HTML标签的正则表达式
    /// </summary>
    private const string RemoveHtmlTagsRegex = @"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>";

    /// <summary>
    /// 用于从HTML中移除注释的正则表达式
    /// </summary>
    private static readonly Regex RemoveHtmlCommentsRegex = new("<!--.*?-->", RegexOptions.Singleline);

    /// <summary>
    /// 用于从HTML中移除脚本的正则表达式
    /// </summary>
    private static readonly Regex RemoveHtmlScriptsRegex = new(@"(?s)<script.*?(/>|</script>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary>
    /// 用于从HTML中移除样式的正则表达式
    /// </summary>
    private static readonly Regex RemoveHtmlStylesRegex = new(@"(?s)<style.*?(/>|</style>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary>
    /// 判断字符串是否为有效的电子邮件地址
    /// </summary>
    /// <param name="str">要测试的字符串</param>
    /// <returns>如果是有效的电子邮件地址则返回<c>true</c>；否则返回<c>false</c></returns>
    public static bool IsEmail(this string str) => Regex.IsMatch(str, EmailRegex);

    /// <summary>
    /// 判断字符串是否为有效的十进制数
    /// </summary>
    /// <param name="str">要测试的字符串</param>
    /// <returns>如果是有效的十进制数则返回<c>true</c>；否则返回<c>false</c></returns>
    public static bool IsDecimal([NotNullWhen(true)] this string? str)
    {
        return decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// 判断字符串是否为有效的整数
    /// </summary>
    /// <param name="str">要测试的字符串</param>
    /// <returns>如果是有效的整数则返回<c>true</c>；否则返回<c>false</c></returns>
    public static bool IsNumeric([NotNullWhen(true)] this string? str)
    {
        return int.TryParse(str, out _);
    }

    /// <summary>
    /// 判断字符串是否为有效的电话号码
    /// </summary>
    /// <param name="str">要测试的字符串</param>
    /// <returns>如果是有效的电话号码则返回<c>true</c>；否则返回<c>false</c></returns>
    public static bool IsPhoneNumber(this string str) => Regex.IsMatch(str, PhoneNumberRegex);

    /// <summary>
    /// 判断字符串是否只包含字母
    /// </summary>
    /// <param name="str">要测试的字符串</param>
    /// <returns>如果字符串只包含字母则返回<c>true</c>；否则返回<c>false</c></returns>
    public static bool IsCharacterString(this string str) => Regex.IsMatch(str, CharactersRegex);

    /// <summary>
    /// 返回一个去除HTML注释、脚本、样式和标签的字符串
    /// </summary>
    /// <param name="htmlText">HTML字符串</param>
    /// <returns>解码后的HTML字符串</returns>
    [return: NotNullIfNotNull(nameof(htmlText))]
    public static string? DecodeHtml(this string? htmlText)
    {
        if (htmlText is null)
        {
            return null;
        }

        string? ret = htmlText.FixHtml();

        // 移除HTML标签
        ret = new Regex(RemoveHtmlTagsRegex).Replace(ret, string.Empty);

        return WebUtility.HtmlDecode(ret);
    }

    /// <summary>
    /// 返回一个去除HTML注释、脚本和样式的字符串
    /// </summary>
    /// <param name="html">要修复的HTML字符串</param>
    /// <returns>修复后的HTML字符串</returns>
    public static string FixHtml(this string html)
    {
        // 移除注释
        string? withoutComments = RemoveHtmlCommentsRegex.Replace(html, string.Empty);

        // 移除脚本
        string? withoutScripts = RemoveHtmlScriptsRegex.Replace(withoutComments, string.Empty);

        // 移除样式
        string? withoutStyles = RemoveHtmlStylesRegex.Replace(withoutScripts, string.Empty);

        return withoutStyles;
    }

    /// <summary>
    /// 将字符串截断为指定长度
    /// </summary>
    /// <param name="value">要截断的字符串</param>
    /// <param name="length">最大长度</param>
    /// <returns>截断后的字符串</returns>
    public static string Truncate(this string? value, int length) => Truncate(value, length, false);

    /// <summary>
    /// 将字符串截断为指定长度
    /// </summary>
    /// <param name="value">要截断的字符串</param>
    /// <param name="length">最大长度</param>
    /// <param name="ellipsis">如果为<c>true</c>，则在截断文本后添加省略号；否则为<c>false</c></param>
    /// <returns>截断后的字符串</returns>
    public static string Truncate(this string? value, int length, bool ellipsis)
    {
        if (!string.IsNullOrEmpty(value))
        {
            value = value!.Trim();

            if (value.Length > length)
            {
                if (ellipsis)
                {
                    return value.Substring(0, length) + "...";
                }

                return value.Substring(0, length);
            }
        }

        return value ?? string.Empty;
    }
}