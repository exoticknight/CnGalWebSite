﻿using CnGalWebSite.DataModel.ExamineModel;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel;
using CnGalWebSite.DataModel.ViewModel.Perfections;
using CnGalWebSite.DataModel.ViewModel.Votes;
using CnGalWebSite.Helper.Extensions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CnGalWebSite.DataModel.Helper
{
    public static class ToolHelper
    {
        //http://localhost:45160/";
        //public const string WebApiPath = "http://172.19.160.1:45160/";
        //http://localhost:51313/
        //http://101.34.84.38:2001/
        //121.43.54.210
        //http://172.17.0.1:2001/
        //https://v3.cngal.org/


        //public const string WebApiPath = "http://localhost:45160/";
        public const string WebApiPath = "http://172.17.0.1:2001/";
        //public const string WebApiPath = "https://www.cngal.org/";

        public static bool IsSSR => WebApiPath == "http://172.17.0.1:2001/";
        public static bool IsWASM => WebApiPath == "https://www.cngal.org/";

        public const string ImageApiPath = "https://api.cngal.top/";
        //public const string ImageApiPath = "http://localhost:5098/";


        public const bool IsMaui = false;
        public const bool IsApp = true;

        public static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public const int MaxEditorCount = 200;
        public const int MinValidPlayImpressionsLength = 30;

        //临时储存的信息



        public static List<string> GetImageLinks(string context)
        {
            var result = new List<string>();
            //查找符合markdown语法的图片
            var linshi = context.Split('!');
            for (var i = 1; i < linshi.Length; i++)
            {
                try
                {
                    var temp = MidStrEx(linshi[i], "[image](", ")");
                    if (string.IsNullOrWhiteSpace(temp))
                    {
                        temp = MidStrEx(linshi[i], "[image.png](", ")");
                    }
                    if (string.IsNullOrWhiteSpace(temp))
                    {
                        temp = MidStrEx(linshi[i], "[](", ")");
                    }
                    if (string.IsNullOrWhiteSpace(temp) == false && temp.Contains("data:image") == false)
                    {
                        result.Add(temp);
                    }
                }
                catch
                {

                }

            }
            //查找符合html语法的图片
            linshi = context.Split("<img");
            if (linshi.Length > 1)
            {
                var linshi2 = new List<string>();
                for (var i = 1; i < linshi.Length; i++)
                {
                    var linshi3 = linshi[i].Split(">");
                    if (linshi3.Length >= 1)
                    {
                        linshi2.Add(linshi3[0]);
                    }
                }
                //提取
                foreach (var item in linshi2)
                {
                    try
                    {
                        var temp = MidStrEx(item, "src=\"", "\"");
                        if (string.IsNullOrWhiteSpace(temp) == false && temp.Contains("data:image") == false)
                        {
                            result.Add(temp);
                        }
                    }
                    catch
                    {

                    }
                }

            }
            return result;
        }

        public static string MidStrEx(string sourse, string startstr, string endstr)
        {
            var result = string.Empty;
            int startindex, endindex;
            try
            {
                startindex = sourse.IndexOf(startstr);
                if (startindex == -1)
                {
                    return result;
                }

                var tmpstr = sourse[(startindex + startstr.Length)..];
                endindex = tmpstr.IndexOf(endstr);
                if (endindex == -1)
                {
                    return result;
                }

                result = tmpstr.Remove(endindex);
            }
            catch
            {
                //Log.WriteLog("MidStrEx Err:" + ex.Message);
            }
            return result;
        }

        public static string Base64EncodeName(string content)
        {

            var bytes = Encoding.UTF8.GetBytes(content);
            return "A" + Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
        }

        public static string Base64DecodeName(string content)
        {
            content = content[1..];
            content = content.Replace("-", "+").Replace("_", "/");
            var bytes = Convert.FromBase64String(content);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string Base64EncodeUrl(string content)
        {

            var bytes = Encoding.UTF8.GetBytes(content);
            return "A" + Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
        }

        public static string Base64DecodeUrl(string content)
        {
            content = content[1..];
            content = content.Replace("-", "+").Replace("_", "/").Replace("%3d", "=").Replace("%3D", "=");
            var bytes = Convert.FromBase64String(content);
            var result = Encoding.UTF8.GetString(bytes);
            //判断Url是否是本地连接
            if (string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
            else
            {
                if (result.StartsWith("/"))
                {
                    return result;
                }
                else
                {
                    if (result.StartsWith("https://app.cngal.org") || result.StartsWith("http://app.cngal.org") || result.StartsWith("https://m.cngal.org") || result.StartsWith("http://m.cngal.org") || result.StartsWith("https://www.cngal.org") || result.StartsWith("http://www.cngal.org") || result.StartsWith("https://localhost:") || result.StartsWith("http://localhost:"))
                    {
                        return result;
                    }
                    else
                    {
                        return "/";
                    }

                }
            }
        }

        public static string Base64EncodeString(string content)
        {
            if (content == null)
            {
                return null;
            }
            var bytes = Encoding.UTF8.GetBytes(content);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
        }

        public static string Base64DecodeString(string content)
        {
            if (content == null)
            {
                return null;
            }
            content = content.Replace("-", "+").Replace("_", "/");
            switch (content.Length % 4)
            {
                case 2: content += "=="; break;
                case 3: content += "="; break;
            }
            var bytes = Convert.FromBase64String(content);
            return Encoding.UTF8.GetString(bytes);
        }


        /// <summary>
        /// 计算文件的 MD5 值
        /// </summary>
        /// <param name="FileName">要计算 Sha1 值的文件名和路径</param>
        /// <returns>MD5 值16进制字符串</returns>
        public static string ComputeFileSHA1(string FileName)
        {
            try
            {
                // 创建Hash算法对象
                byte[] hr; using (var Hash = new SHA1Managed()) // 创建文件流对象
                {
                    using var fs = new FileStream(FileName, FileMode.Open);
                    hr = Hash.ComputeHash(fs);
                    // 计算
                }
                return BitConverter.ToString(hr).Replace("-", "");
                // 转化为十六进制字符串
            }
            catch (IOException) { return "Error:访问文件时出现异常"; }
        }

        /// <summary>
        /// 计算文件的 MD5 值
        /// </summary>
        /// <param name="FileName">要计算 Sha1 值的文件名和路径</param>
        /// <returns>MD5 值16进制字符串</returns>
        public static async Task<string> ComputeFileSHA1(Stream fs)
        {
            try
            {
                // 创建Hash算法对象
                byte[] hr;
                using (var Hash = new SHA1Managed()) // 创建文件流对象
                {

                    hr = await Hash.ComputeHashAsync(fs);
                    // 计算
                }
                return BitConverter.ToString(hr).Replace("-", "");
                // 转化为十六进制字符串
            }
            catch (IOException) { return "Error:访问文件时出现异常"; }
        }

        public static string GetFileBase64(Stream fsForRead)
        {
            string base64Str;
            try
            {
                //读入一个字节
                //读写指针移到距开头10个字节处
                fsForRead.Seek(0, SeekOrigin.Begin);
                var bs = new byte[fsForRead.Length];
                var log = Convert.ToInt32(fsForRead.Length);
                //从文件中读取10个字节放到数组bs中
                fsForRead.Read(bs, 0, log);
                base64Str = Convert.ToBase64String(bs);

                return base64Str;
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
            finally
            {
                fsForRead.Close();
            }
        }

        /// <summary>
        /// 文件转为base64编码
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<string> FileToBase64Str(Stream fsForRead)
        {
            var base64Str = string.Empty;
            try
            {

                var bt = new byte[fsForRead.Length];

                //调用read读取方法

                await fsForRead.ReadAsync(bt.AsMemory(0, bt.Length));
                base64Str = Convert.ToBase64String(bt);
                fsForRead.Close();
                return base64Str;
            }
            catch
            {
                return base64Str;
            }
        }

        public static string GetFileBase64(string path)
        {
            var fsForRead = new FileStream(path, FileMode.Open);
            string base64Str;
            try
            {
                //读入一个字节
                //读写指针移到距开头10个字节处
                fsForRead.Seek(0, SeekOrigin.Begin);
                var bs = new byte[fsForRead.Length];
                var log = Convert.ToInt32(fsForRead.Length);
                //从文件中读取10个字节放到数组bs中
                fsForRead.Read(bs, 0, log);
                base64Str = Convert.ToBase64String(bs);

                return base64Str;
            }
            catch
            {
            }
            finally
            {
                fsForRead.Close();
            }
            return null;
        }
        /// <summary>
        /// 批量导入抽奖奖项
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>

        public static List<string> GetTextListFromString(string text)
        {
            var result = new List<string>();

            text = text.Replace("，", ",").Replace("、", ",").Replace("：", ":").Replace("\r\n", "\n");
            var Subcategory = string.Empty;
            //按行分割
            var lines = text.Split('\n');

            foreach (var item in lines)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }
                result.Add(item);

            }

            return result;
        }

        /// <summary>
        /// 批量导入Staff信息
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<StaffModel> GetStaffsFromString(string text)
        {
            var result = new List<StaffModel>();

            text = text.Replace("，", ",").Replace("、", ",").Replace("：", ":").Replace("（", "(").Replace("）", ")").Replace("\r\n", "\n");
            var Subcategory = string.Empty;
            //按行分割
            var lines = text.Split('\n');

            foreach (var item in lines)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }
                //按冒号分割
                var pairs = item.Split(":");
                string Position;
                string PositionGeneral;
                if (pairs.Length == 0)
                {
                    continue;
                }
                else if (pairs.Length == 1 || (pairs.Length == 2 && string.IsNullOrWhiteSpace(pairs[1])))
                {
                    Subcategory = pairs[0];
                    continue;
                }
                else if (pairs.Length == 2)
                {
                    Position = pairs[0];
                    PositionGeneral = MidStrEx(Position, "(", ")");
                    if (string.IsNullOrWhiteSpace(PositionGeneral))
                    {
                        PositionGeneral = null;
                    }
                    else
                    {
                        Position = Position.Replace($"({PositionGeneral})", "");
                    }
                }
                else
                {
                    continue;
                }
                //分割名字
                var names = pairs[1].Split(",");
                foreach (var infor in names)
                {
                    if (string.IsNullOrWhiteSpace(infor))
                    {
                        continue;
                    }
                    var roleName = MidStrEx(infor, "(", ")");
                    //创建staff
                    var type = GetGeneralType(PositionGeneral ?? Position);
                    result.Add(new StaffModel
                    {
                        NicknameOfficial = infor.Replace($"({roleName})", ""),
                        PositionOfficial = Position,
                        Subcategory = Subcategory,
                        Role = type == PositionGeneralType.CV ? roleName : null,
                        SubordinateOrganization = type == PositionGeneralType.CV ? null : roleName,
                        PositionGeneral = type
                    });
                }

            }

            return result;
        }
        /// <summary>
        /// 批量导入投票选项
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<EditVoteOptionModel> GetOptionsFromString(string text)
        {
            var result = new List<EditVoteOptionModel>();

            text = text.Replace("，", ",").Replace("、", ",").Replace("：", ":").Replace("\r\n", "\n");
            var Subcategory = "文本";
            //按行分割
            var lines = text.Split('\n');

            foreach (var item in lines)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }
                //按冒号分割
                var pairs = item.Split(":");
                if (pairs.Length == 0)
                {
                    continue;
                }
                else if ((pairs.Length == 1 || (pairs.Length == 2 && string.IsNullOrWhiteSpace(pairs[1]))) && item.Contains(":"))
                {
                    Subcategory = pairs[0];
                    continue;
                }


                //分割名字
                var name = pairs[0];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                //创建
                result.Add(new EditVoteOptionModel
                {
                    Text = name,
                    Type = Subcategory.ToEnumValue<VoteOptionType>()
                });


            }

            return result;
        }

        public static PositionGeneralType GetGeneralType(string text)
        {
            foreach (var item in Enum.GetValues(typeof(PositionGeneralType)))
            {
                var temp = (PositionGeneralType)item;
                if (text == temp.GetDisplayName())
                {
                    return temp;
                }
            }

            //查找其他符合的文本
            if (text.Contains("原画") || text.Contains("画师"))
            {
                return PositionGeneralType.FineArts;
            }
            else if (text.Contains("设计"))
            {
                return PositionGeneralType.FineArts;
            }
            else if (text.Contains("配音") || text.ToUpper().Contains("CV"))
            {
                return PositionGeneralType.CV;
            }

            return PositionGeneralType.Other;
        }

        public static T ToEnumValue<T>(this string text) where T : Enum
        {
            foreach (var item in Enum.GetValues(typeof(T)))
            {
                var temp = (T)item;
                if (text == temp.GetDisplayName())
                {
                    return temp;
                }
            }
            return default;
        }

        /// <summary>
        /// 检查手机号是否正确
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool CheckPhoneIsAble(string input)
        {
            if (input.Length < 11)
            {
                return false;
            }
            //电信手机号码正则
            var dianxin = @"^1[3578][01379]\d{8}$";
            var regexDX = new Regex(dianxin);
            //联通手机号码正则
            var liantong = @"^1[34578][01256]\d{8}";
            var regexLT = new Regex(liantong);
            //移动手机号码正则
            var yidong = @"^(1[012345678]\d{8}|1[345678][012356789]\d{8})$";
            var regexYD = new Regex(yidong);
            if (regexDX.IsMatch(input) || regexLT.IsMatch(input) || regexYD.IsMatch(input))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 时间戳转为C#格式时间10位
        /// </summary>
        /// <param name="curSeconds">Unix时间戳格式</param>
        /// <returns>C#格式时间</returns>
        public static DateTime GetDateTimeFrom1970Ticks(long curSeconds)
        {
            var dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            return dtStart.AddSeconds(curSeconds);
        }

        /// <summary>
        /// 返回隐藏中间的字符串
        /// </summary>
        /// <param name="Input">输入</param>
        /// <returns>输出</returns>
        public static string GetxxxString(string Input)
        {
            if (string.IsNullOrEmpty(Input))
            {
                return "";
            }
            var length = Input.Length / 2;
            string Output;
            switch (Input.Length)
            {
                case 1:
                    Output = "*";
                    break;
                case 2:
                    Output = Input[0] + "*";
                    break;
                case 0:
                    Output = "";
                    break;
                default:
                    Output = Input[..(length / 2)];
                    for (var i = 0; i < ((double)Input.Length / 2); i++)
                    {
                        Output += "*";
                    }
                    Output += Input.Substring(Input.Length - length / 2, length / 2);
                    break;
            }
            return Output;
        }

        public static string GetThirdPartyLoginUrl(string returnUrl, ThirdPartyLoginType type)
        {
            var callbackUrl = GetThirdPartyCallbackUrl(type);

            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = Base64EncodeUrl("/");
            }

            return type switch
            {
                ThirdPartyLoginType.Microsoft => "https://login.microsoftonline.com/5486ee26-7c6a-4246-8776-1f0d104f861a/oauth2/authorize?response_type=code&redirect_uri=" + callbackUrl + "&state=" + returnUrl + "&client_id=a579e682-d4fc-45ff-a77d-96883700df69",
                ThirdPartyLoginType.GitHub => "https://github.com/login/oauth/authorize?client_id=279b4ff2b18e5db533c5&redirect_uri=" + callbackUrl + "&state=" + returnUrl,
                ThirdPartyLoginType.Gitee => "https://gitee.com/oauth/authorize?client_id=f478615d7838c3a5884d71bf68d0b69032e46e06f6648e75f778ccb96e70e5ad&redirect_uri=" + callbackUrl + "&response_type=code&state=" + returnUrl,
                ThirdPartyLoginType.QQ => "https://graph.qq.com/oauth2.0/authorize?response_type=code&client_id=101971417&redirect_uri=" + callbackUrl + "&state=" + returnUrl + "&scope=get_user_info,getUnionId",
                _ => null,
            };
        }

        public static string GetThirdPartyCallbackUrl(ThirdPartyLoginType type)
        {
            var callbackUrl = IsSSR ? "https://www.cngal.org/account/" : "https://app.cngal.org/account/";
            //string callbackUrl = "http://localhost:3001/account/";
            switch (type)
            {
                case ThirdPartyLoginType.Microsoft:
                    callbackUrl += "microsoftlogin/";
                    return callbackUrl;
                case ThirdPartyLoginType.GitHub:
                    callbackUrl += "githublogin/";
                    return callbackUrl;
                case ThirdPartyLoginType.Gitee:
                    callbackUrl += "giteelogin/";
                    return callbackUrl;
                case ThirdPartyLoginType.QQ:
                    callbackUrl += "qqlogin";
                    return callbackUrl;
            }

            return null;
        }

        public static PerfectionLevel GetEntryPerfectionLevel(double grade)
        {
            if (grade < 60)
            {
                return PerfectionLevel.ToBeImproved;
            }
            else if (grade < 80)
            {
                return PerfectionLevel.Good;
            }
            else
            {
                return PerfectionLevel.Excellent;
            }
        }

        public static string GetEntryPerfectionLevelColor(PerfectionLevel level)
        {
            if (level == PerfectionLevel.ToBeImproved)
            {
                return "danger";
            }
            else if (level == PerfectionLevel.Good)
            {
                return "primary";
            }
            else
            {
                return "success";
            }
        }

        public static PerfectionCheckLevel GetEntryPerfectionCheckLevel(PerfectionCheckType checkType, PerfectionDefectType defectType)
        {
            if (defectType == PerfectionDefectType.None)
            {
                return PerfectionCheckLevel.None;
            }
            else if (checkType == PerfectionCheckType.BriefIntroduction || checkType == PerfectionCheckType.MainImage
                || checkType == PerfectionCheckType.IssueTime || checkType == PerfectionCheckType.MainPage)
            {
                return PerfectionCheckLevel.High;
            }
            else if (checkType == PerfectionCheckType.Staff || checkType == PerfectionCheckType.SteamId
                || checkType == PerfectionCheckType.ProductionGroup)
            {
                return PerfectionCheckLevel.Middle;
            }
            else
            {
                return PerfectionCheckLevel.Low;
            }
        }

        public static string GetEntryPerfectionCheckLevelColor(PerfectionCheckLevel level)
        {
            if (level == PerfectionCheckLevel.None)
            {
                return "success";
            }
            else if (level == PerfectionCheckLevel.High)
            {
                return "danger";
            }
            else if (level == PerfectionCheckLevel.Middle)
            {
                return "warning";
            }
            else
            {
                return "primary";
            }
        }

        public static string GetPerfectionCheckTitle(PerfectionCheckViewModel model)
        {
            return model.DefectType switch
            {
                PerfectionDefectType.None => model.CheckType.GetDisplayName() + " 已填写",
                _ => model.CheckType switch
                {
                    PerfectionCheckType.RelevanceEntries => string.IsNullOrWhiteSpace(model.Infor) ? (model.CheckType.GetDisplayName() + " " + model.DefectType.GetDisplayName()) : ("关联词条 " + model.Infor + " " + model.DefectType.GetDisplayName()),
                    PerfectionCheckType.RelevanceArticles => string.IsNullOrWhiteSpace(model.Infor) ? (model.CheckType.GetDisplayName() + " " + model.DefectType.GetDisplayName()) : ("关联文章 " + model.Infor + " " + model.DefectType.GetDisplayName()),
                    PerfectionCheckType.ProductionGroup => string.IsNullOrWhiteSpace(model.Infor) ? (model.CheckType.GetDisplayName() + " " + model.DefectType.GetDisplayName()) : ("制作组 " + model.Infor + " " + model.DefectType.GetDisplayName()),
                    PerfectionCheckType.Publisher => string.IsNullOrWhiteSpace(model.Infor) ? (model.CheckType.GetDisplayName() + " " + model.DefectType.GetDisplayName()) : ("发行商 " + model.Infor + " " + model.DefectType.GetDisplayName()),
                    PerfectionCheckType.Staff => string.IsNullOrWhiteSpace(model.Infor) ? (model.CheckType.GetDisplayName() + " " + model.DefectType.GetDisplayName()) : ("关联Staff " + model.Infor + " " + model.DefectType.GetDisplayName()),
                    _ => model.CheckType.GetDisplayName() + " " + model.DefectType.GetDisplayName(),
                },
            };
        }

        public static string GetPerfectionCheckContext(PerfectionCheckViewModel model)
        {
            return model.DefectType switch
            {
                PerfectionDefectType.InsufficientLength => model.CheckType switch
                {
                    PerfectionCheckType.BriefIntroduction => "简介长度建议30字以上，可以多写一些游戏的介绍哦",
                    PerfectionCheckType.MainPage => "主页长度建议100字以上，可以介绍一下游戏的故事背景，出场角色等内容哦",
                    _ => model.CheckType.GetDisplayName() + "建议多写一些哦",
                },
                PerfectionDefectType.None => "全站 " + model.VictoryPercentage.ToString("0.0") + "% 的词条已填写此项",
                PerfectionDefectType.TypeError => "关联的 " + model.Infor + " 类型错误，请检查是否重名",
                _ => model.CheckType switch
                {
                    PerfectionCheckType.RelevanceEntries => string.IsNullOrWhiteSpace(model.Infor) ? ("需要 填写关联词条") : (model.Count > 1 ? ("全站约有" + (model.Count - 1).ToString() + "个词条也关联了 " + model.Infor) : ("全站约有" + model.Count + "个词条关联了 " + model.Infor)),
                    PerfectionCheckType.RelevanceArticles => string.IsNullOrWhiteSpace(model.Infor) ? ("需要 填写关联文章") : (model.Count > 1 ? ("全站约有" + (model.Count - 1).ToString() + "个词条也关联了 " + model.Infor) : ("全站约有" + model.Count + "个词条关联了 " + model.Infor)),
                    PerfectionCheckType.ProductionGroup => string.IsNullOrWhiteSpace(model.Infor) ? ("需要 填写制作组") : (model.Count > 1 ? ("全站约有" + (model.Count - 1).ToString() + "个词条也关联了 " + model.Infor) : ("全站约有" + model.Count + "个词条关联了 " + model.Infor)),
                    PerfectionCheckType.Publisher => string.IsNullOrWhiteSpace(model.Infor) ? ("需要 填写发行商") : (model.Count > 1 ? ("全站约有" + (model.Count - 1).ToString() + "个词条也关联了 " + model.Infor) : ("全站约有" + model.Count + "个词条关联了 " + model.Infor)),
                    PerfectionCheckType.Staff => string.IsNullOrWhiteSpace(model.Infor) ? ("需要 填写关联Staff") : (model.Count > 1 ? ("全站约有" + (model.Count - 1).ToString() + "个词条也关联了 " + model.Infor) : ("全站约有" + model.Count + "个词条关联了 " + model.Infor)),
                    _ => "需要填写 " + model.CheckType.GetDisplayName(),
                },
            };
        }

        /// <summary>
        /// 清除重复字符串
        /// </summary>
        /// <param name="needToPurge"></param>
        public static void Purge(ref List<string> needToPurge)
        {

            for (var i = 0; i < needToPurge.Count - 1; i++)
            {
                var deststring = needToPurge[i];
                for (var j = i + 1; j < needToPurge.Count; j++)
                {
                    if (deststring.CompareTo(needToPurge[j]) == 0)
                    {
                        needToPurge.RemoveAt(j);
                        j--;
                        continue;
                    }
                }
            }
        }

        public static List<string> FindStringListInText(string text, List<string> strings)
        {
            var list = new List<string>();
            foreach (var str in strings)
            {
                if (text.Contains(str))
                {
                    list.Add(str);
                }
            }

            return list;
        }

        public static int GetLevelIntegral(int level)
        {
            return level switch
            {
                0 => 5,
                1 => 50,
                2 => 200,
                3 => 500,
                4 => 1000,
                5 => 2000,
                6 => 3500,
                7 => 6000,
                8 => 10000,
                9 => 99999999,
                10 => 99999999,
                _ => -1,
            };
        }

        public static int GetUserLevel(int integral)
        {
            for (var i = 0; i <= 10; i++)
            {
                if (integral < GetLevelIntegral(i))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 是否在同一个星期
        /// </summary>
        /// <param name="day1"></param>
        /// <param name="day2"></param>
        /// <returns></returns>
        public static bool IsInSameWeek(this DateTime day1, DateTime day2)
        {
            var temp1 = day1.AddDays(-(int)day1.DayOfWeek).Date;
            var temp2 = day2.AddDays(-(int)day2.DayOfWeek).Date;

            return temp1 == temp2;
        }

        public static void ModifyDataAccordingToEditingRecord<TResult>(TResult data, List<ExamineMainAlone> examines) where TResult : class
        {
            var t = data.GetType();
            foreach (var item in examines)
            {
                var pt = t.GetProperty(item.Key);
                if (pt != null)
                {
                    if (pt.PropertyType == typeof(EntryType))
                    {
                        pt.SetValue(data, (EntryType)int.Parse(item.Value), null);
                    }
                    else if (pt.PropertyType == typeof(ArticleType))
                    {
                        pt.SetValue(data, (ArticleType)int.Parse(item.Value), null);
                    }
                    else if (pt.PropertyType == typeof(PeripheryType))
                    {
                        pt.SetValue(data, (PeripheryType)int.Parse(item.Value), null);
                    }
                    else if (pt.PropertyType == typeof(DateTime))
                    {
                        pt.SetValue(data, DateTime.FromBinary(long.Parse(item.Value)), null);
                    }
                    else if (pt.PropertyType == typeof(DateTime?))
                    {
                        pt.SetValue(data, item.Value != null ? DateTime.FromBinary(long.Parse(item.Value)) : null, null);
                    }
                    else if (pt.PropertyType == typeof(string))
                    {
                        pt.SetValue(data, item.Value, null);
                    }
                    else if (pt.PropertyType == typeof(bool))
                    {
                        pt.SetValue(data, item.Value != null ? bool.Parse(item.Value) : false, null);
                    }
                    else if (pt.PropertyType == typeof(int))
                    {
                        pt.SetValue(data, item.Value != null ? int.Parse(item.Value) : 0, null);
                    }
                }
            }
        }

        public static List<ExamineMainAlone> GetEditingRecordFromContrastData<TResult>(TResult currentItem, TResult newItem) where TResult : class
        {
            var model = new List<ExamineMainAlone>();

            var t = currentItem.GetType();
            var pts = t.GetProperties();
            foreach (var item in pts.Where(s => s.PropertyType == typeof(string) || s.PropertyType == typeof(DateTime) || s.PropertyType == typeof(DateTime?)
            || s.PropertyType == typeof(EntryType) || s.PropertyType == typeof(ArticleType) || s.PropertyType == typeof(PeripheryType) || s.PropertyType == typeof(bool) || s.PropertyType == typeof(int)))
            {
                //特殊字段跳过
                if (item.Name == "MainPage" || item.Name == "CreateTime" || item.Name == "LastEditTime" || item.Name == "CreateUserId")
                {
                    continue;
                }
                if (item.PropertyType == typeof(bool) && item.Name != "IsReprint" && item.Name != "IsAvailableItem")
                {
                    continue;
                }
                if (item.PropertyType == typeof(int) && item.Name != "PageCount" && item.Name != "SongCount")
                {
                    continue;
                }

                var currentValue = item.GetValue(currentItem);
                var newValue = item.GetValue(newItem);

                if (currentValue == null && newValue == null)
                {
                    continue;
                }

                if (currentValue == null || newValue == null || currentValue.ToString() != newValue.ToString())
                {
                    if (item.PropertyType == typeof(DateTime))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = ((DateTime)newValue).ToBinary().ToString()
                        });
                    }
                    if (item.PropertyType == typeof(DateTime?))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = ((DateTime?)newValue)?.ToBinary().ToString()
                        });
                    }
                    else if (item.PropertyType == typeof(ArticleType))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = ((int)(ArticleType)newValue).ToString()
                        });
                    }
                    else if (item.PropertyType == typeof(EntryType))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = ((int)(EntryType)newValue).ToString()
                        });
                    }
                    else if (item.PropertyType == typeof(PeripheryType))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = ((int)(PeripheryType)newValue).ToString()
                        });
                    }
                    else if (item.PropertyType == typeof(string))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = (string)newValue
                        });
                    }
                    else if (item.PropertyType == typeof(bool))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = newValue.ToString()
                        });
                    }
                    else if (item.PropertyType == typeof(int))
                    {
                        model.Add(new ExamineMainAlone
                        {
                            Key = item.Name,
                            Value = newValue.ToString()
                        });
                    }
                }
            }

            return model;
        }

        /// <summary>
        /// 词条附加信息 列表 清理相同项目
        /// </summary>
        /// <param name="informations"></param>
        /// <returns></returns>
        public static List<BasicEntryInformation> Purge(this List<BasicEntryInformation> informations)
        {
            var list = informations.ToList();
            foreach (var item in list)
            {
                if (informations.Count(s => item.DisplayName == s.DisplayName && item.DisplayValue == s.DisplayValue && item.Modifier == s.Modifier) > 1)
                {
                    var temp = informations.FirstOrDefault(s => item.DisplayName == s.DisplayName && s.DisplayValue == s.DisplayValue && item.Modifier == s.Modifier);
                    if (temp != null)
                    {
                        informations.Remove(temp);
                    }
                }
            }

            return informations;
        }

        /// <summary>
        /// 词条 关联词条 列表 清理相同项目
        /// </summary>
        /// <param name="informations"></param>
        /// <returns></returns>
        public static List<EntryRelation> Purge(this List<EntryRelation> informations)
        {
            var list = informations.ToList();
            foreach (var item in list)
            {
                if (informations.Count(s => item.ToEntry == s.ToEntry) > 1)
                {
                    var temp = informations.FirstOrDefault(s => item.ToEntry == s.ToEntry);
                    if (temp != null)
                    {
                        informations.Remove(temp);
                    }
                }
            }

            return informations;
        }
        /// <summary>
        /// 周报 关联周报 列表 清理相同项目
        /// </summary>
        /// <param name="informations"></param>
        /// <returns></returns>
        public static List<PeripheryRelation> Purge(this List<PeripheryRelation> informations)
        {
            var list = informations.ToList();
            foreach (var item in list)
            {
                if (informations.Count(s => item.ToPeriphery == s.ToPeriphery) > 1)
                {
                    var temp = informations.FirstOrDefault(s => item.ToPeriphery == s.ToPeriphery);
                    if (temp != null)
                    {
                        informations.Remove(temp);
                    }
                }
            }

            return informations;
        }

        /// <summary>
        /// 词条 附加信息 的 额外信息列表 清理相同项目
        /// </summary>
        /// <param name="informations"></param>
        /// <returns></returns>
        public static List<BasicEntryInformationAdditional> Purge(this List<BasicEntryInformationAdditional> informations)
        {
            var list = informations.ToList();
            foreach (var item in list)
            {
                if (informations.Count(s => item.DisplayName == s.DisplayName) > 1)
                {
                    var temp = informations.FirstOrDefault(s => item.DisplayValue == s.DisplayValue);
                    if (temp != null)
                    {
                        informations.Remove(temp);
                    }
                }
            }

            return informations;
        }

        /// <summary>
        /// 词条 相册 的 列表 清理相同项目
        /// </summary>
        /// <param name="informations"></param>
        /// <returns></returns>
        public static List<EntryPicture> Purge(this List<EntryPicture> informations)
        {
            var list = informations.ToList();
            foreach (var item in list)
            {
                if (informations.Count(s => item.Url == s.Url) > 1)
                {
                    var temp = informations.FirstOrDefault(s => item.Url == s.Url);
                    if (temp != null)
                    {
                        informations.Remove(temp);
                    }
                }
            }

            return informations;
        }

        public static string GetImagePath(string image, string defaultStr, bool mediumImage = false)
        {

            if (string.IsNullOrWhiteSpace(image) == true)
            {
                if (string.IsNullOrWhiteSpace(defaultStr) == true)
                {
                    return "";
                }
                else
                {
                    image = "default/" + defaultStr;
                    return "https://app.cngal.org/_content/CnGalWebSite.Shared/images/" + image;
                }

            }
            else
            {
                //判断是否为绝对路径
                if (image.Contains("http://") || image.Contains("https://") || image.Contains("//"))
                {
                    //if(image.Contains("pic.cngal.top"))
                    //{
                    //    return image.Replace(".png", ".md.png").Replace(".jpg", ".md.jpg").Replace(".jpeg", ".md.jpeg").Replace(".gif", ".md.gif");
                    //}

                    if (image.Contains("http://pic.cngal.top"))
                    {
                        return image.Replace("http://pic.cngal.top", "https://image.cngal.org");
                    }

                    return image;
                }
                else
                {
                    //http://localhost:51313/
                    //121.43.54.210
                    return "https://app.cngal.org/_content/CnGalWebSite.Shared/images/" + image;
                }
            }
        }


    }
        public class QueryPageOptionsHelper : CnGalWebSite.DataModel.ViewModel.Search.QueryPageOptions
        {
            /// <summary>
            /// 隐式转换
            /// </summary>
            /// <param name="cat"></param>
            public static implicit operator QueryPageOptionsHelper(BootstrapBlazor.Components.QueryPageOptions cat)
            {
                return new QueryPageOptionsHelper
                {
                    SearchText = cat.SearchText,
                    SortName = cat.SortName,
                    SortOrder = (CnGalWebSite.DataModel.ViewModel.Search.SortOrder)cat.SortOrder,
                    SearchModel = cat.SearchModel,
                    PageIndex = cat.PageIndex,
                    StartIndex = cat.StartIndex,
                    PageItems = cat.PageItems,
                    IsPage = cat.IsPage,
                };
            }
        }

    
}
