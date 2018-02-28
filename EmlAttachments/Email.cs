#region License
// <copyright file="Email.cs" company="Infiks">
// 
// EML Extract, extract attachments from .eml files.
// Copyright (c) 2013 Infiks
// 
// This file is part of EML Extract.
// 
// EML Extract is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// EML Extract is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with EML Extract.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <author>Erik van der Veen</author>
// <date>2013-05-03 13:49</date>
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Net.Mail;
using System.Linq;
using System.Web;
using System.Security.Cryptography;



namespace Infiks.Email
{
        
    /// <summary>
    /// A class for representing an email (eml file).
    /// </summary>
    
    public static class StringExtensions
        {
           public static bool Contains(this String str, String substring, 
                                       StringComparison comp)
           {                            
              if (substring == null)
                 throw new ArgumentNullException("substring", 
                                                 "substring cannot be null.");
              else if (! Enum.IsDefined(typeof(StringComparison), comp))
                 throw new ArgumentException("comp is not a member of StringComparison",
                                             "comp");
              //, StringComparison.OrdinalIgnoreCase
              return str.IndexOf(substring, comp) >= 0;                      
           }
        }
    
    class Email
    {
        /// <summary>
        /// The original file name of the .eml file.
        /// </summary>
        
        int attstart = 0;
        public string FileName { private set; get; }
        public string[] boundaries = new string[1];
        bool xtrwhsls_inner = false;

        /// <summary>
        /// The boundary string of the email.
        /// </summary>
        /// 

        public string Boundary
        {
            get { return _boundary ?? (_boundary = GetBoundary()); }
        }

        /// <summary>
        /// The text content of the email.
        /// </summary>
        public string Content
        {
            get { return _content ?? (_content = GetContent()); }
        }

        /// <summary>
        /// The attachments contained in the email.
        /// </summary>
        public IEnumerable<Attachment> Attachments
        {
            get { return _attachments ?? (_attachments = GetAttachments()); }
        }

        /// <summary>
        /// A private variable thats holds the attachments.
        /// </summary>
        private IEnumerable<Attachment> _attachments;

        /// <summary>
        /// A private variable thats holds the boudary string.
        /// </summary>
        private string _boundary;

        /// <summary>
        /// A private variable thats holds the content.
        /// </summary>
        private string _content;

        /// <summary>
        /// Creates a new email object from the specified .eml file.
        /// </summary>
        /// <param name="file">The path to the .eml file.</param>
        public Email(string file)
        {
            // Check if file exists.
            if (!File.Exists(file))
                throw new FileNotFoundException("File not found", file);

            FileName = file;

        }

        /// <summary>
        /// Tries to find the boundary string in the .eml file.
        /// </summary>
        /// <returns>The boundary string if found, otherwise null.</returns>
        private string GetBoundary()
        {
            //there can be more than one boundaries but who fucking cares
            //boundary=\"(.*?)\" 
            Regex regex = new Regex("boundary=\"{0,1}(.*?)[\"\\;\\s\n]", RegexOptions.None);
            Match match = regex.Match(Content);
            //if (match.Success)
                //return match.Groups[match.Groups.Count].Value;
            if (match.Success) {

                int i = 0;
                while (match.Success) {

                    Array.Resize(ref boundaries, i + 1);
                    boundaries[i++] = match.Groups[1].Value;
                    match = match.NextMatch();
                }
                return boundaries[boundaries.Length-1];
            }            
            return null;
        }

        /// <summary>
        /// Gets the text content of the .eml file.
        /// </summary>
        /// <returns>The text content.</returns>
        private string GetContent()
        {
            return File.ReadAllText(FileName);
        }

        private string gettxtcharset() {

            Regex findchrstrgx = new Regex("charset=\"{0,1}(.*?)[\"\\;\\s\n]");

            Match emlcharsetmth = findchrstrgx.Match(Content.Substring(0, attstart));
            string emlcharset = null;
            if (emlcharsetmth.Success)
            {
                emlcharset = emlcharsetmth.Groups[1].Value;
                //emlcharsetmth = emlcharsetmth.NextMatch();
            }

            return emlcharset;
        }

        public static string gettxttransferenc(string tmppart)
        {

            Regex findchrstrgx = new Regex("Content-transfer-encoding: \"{0,1}(.*?)[\"\\;\\s\n]", RegexOptions.IgnoreCase);

            Match trnfenctmth = findchrstrgx.Match(tmppart);
            string trnfenc = "";
            if (trnfenctmth.Success)
                trnfenc = trnfenctmth.Groups[1].Value;

            return trnfenc;
        }

        private string EncodeQuotedPrintable(string value, string chrst)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = new StringBuilder();

            Encoding currenc = Encoding.GetEncoding(chrst);
            //currenc = Encoding.GetEncoding(65001);

            byte[] bytes = currenc.GetBytes(value);
            foreach (byte v in bytes)
            {
                // The following are not required to be encoded:
                // - Tab (ASCII 9)
                // - Space (ASCII 32)
                // - Characters 33 to 126, except for the equal sign (61).

                if ((v == 9) || ((v >= 32) && (v <= 60)) || ((v >= 62) && (v <= 126)))
                {
                    builder.Append(Convert.ToChar(v));
                }
                else
                {
                    builder.Append('=');
                    builder.Append(v.ToString("X2"));
                }
            }

            char lastChar = builder[builder.Length - 1];
            if (char.IsWhiteSpace(lastChar))
            {
                builder.Remove(builder.Length - 1, 1);
                builder.Append('=');
                builder.Append(((int)lastChar).ToString("X2"));
            }

            return builder.ToString();
        }

        public static string DecodeQuotedPrintable(string input, string charSet)
        {
            
            Encoding enc;

            try
            {
                enc = Encoding.GetEncoding(charSet);
            }
            catch
            {
                enc = new UTF8Encoding();
            }

            input = input.Replace("=\r\n=", "=");
            input = input.Replace("=\r\n ", "\r\n ");
            input = input.Replace("= \r\n", " \r\n");
            input = input.Replace("=\r\n", "");
            var occurences = new Regex("");// = new Regex;
            var tmpenc = new UTF8Encoding();

            occurences = new Regex(@"(=[0-9A-Z]{2}){1,}", RegexOptions.Multiline);

            var matches = occurences.Matches(input);

            foreach (Match match in matches)
            {
                try
                {
                    byte[] b = new byte[match.Groups[0].Value.Length / 3];
                    for (int i = 0; i < match.Groups[0].Value.Length / 3; i++)
                    {
                        b[i] = byte.Parse(match.Groups[0].Value.Substring(i * 3 + 1, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    }
                    char[] hexChar = enc.GetChars(b);
                    int z = input.IndexOf(match.Groups[0].Value);
                    input = input.Remove(z, match.Groups[0].Value.Length);
                    input = input.Insert(z, new String(hexChar));
                    
                    //GC.Collect();
                }
                catch
                { Console.WriteLine("QP dec err"); }
            }
            input = input.Replace("?=", ""); 

            return input;
        }

        private string convstr(string origstr, string inchrst, string outchrst)
        {
            
            //Encoding unicode = Encoding.Unicode;

            Encoding destenc = Encoding.GetEncoding(outchrst);

            Encoding currenc = Encoding.GetEncoding(inchrst);

            // Convert the string into a byte array.
            byte[] Bytes = currenc.GetBytes(origstr);

            // Perform the conversion from one encoding to the other.
            byte[] tmpBytes = Encoding.Convert(currenc, destenc, Bytes);

            // Convert the new byte[] into a char[] and then into a string.
            char[] tmpChars = new char[currenc.GetCharCount(tmpBytes, 0, tmpBytes.Length)];
            currenc.GetChars(tmpBytes, 0, tmpBytes.Length, tmpChars, 0);
            return new string(tmpChars);
        }

        private string[] getdateMyyyy() {

            Regex finddatergx = new Regex("^Date:(.*)", RegexOptions.Multiline);
            Match datemth = finddatergx.Match(Content);

            string msgmonthyear = "";

            if (!datemth.Success)
            {
                Console.WriteLine("Date error.");
                Environment.Exit(-9);
            }

            //split by " ", elements index 2 and 3
            msgmonthyear = datemth.Groups[1].Value;
            //string[] strarr = msgmonthyear.Split(' '); //new char[] {' '}, , StringSplitOptions.RemoveEmptyEntries
            string[] strarr = msgmonthyear.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            if (!(new Regex("\\d{4}").Match(strarr[3]).Success) || !(new Regex("[a-zA-Z]{3}").Match(strarr[2]).Success))
            {

                Console.WriteLine("Date parsing error.");
                Environment.Exit(-9);
            }

            string[] dateresult = new string[2];
            dateresult[0] = strarr[3];
            dateresult[1] = strarr[2];

            return dateresult;
        }

        static bool FilesAreEqual_Hash(FileInfo first, FileInfo second)
        {
            byte[] firstHash = MD5.Create().ComputeHash(first.OpenRead());
            byte[] secondHash = MD5.Create().ComputeHash(second.OpenRead());

            for (int i = 0; i < firstHash.Length; i++)
            {
                if (firstHash[i] != secondHash[i])
                    return false;
            }
            return true;
        }


        public static string GetRandomAlphaNumeric()
        {
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(8).ToArray());
        }


        private static List<string> get_tables(string htmlcnt, string tmpchrst)
        {

            List<string> tables = new List<string>();
            string htmltable = "";
            string trnenc = gettxttransferenc(htmlcnt);
            
            //need to find all tables, can be more than one

            //if not base64 coded
            if ( !trnenc.Contains("base64") ) {
                
                if ( !htmlcnt.Contains("<table") ) 
                    return null;
                else {

                    if (trnenc.Contains("quoted-printable"))
                        htmlcnt = DecodeQuotedPrintable(htmlcnt, tmpchrst);
                }
            }
            //if base64 coded
            else {

                htmlcnt = htmlcnt.Substring(htmlcnt.IndexOf("\r\n\r\n") + 4, htmlcnt.Length - htmlcnt.IndexOf("\r\n\r\n") - 4);
                htmlcnt = htmlcnt.Replace("\r\n", null).Replace("--", null);
                htmlcnt = System.Text.Encoding.GetEncoding(tmpchrst).GetString(System.Convert.FromBase64String(htmlcnt));

                if (!htmlcnt.Contains("<table"))
                    return null;
            }
            
            int nth = 0;
            while ( !(nth < 0) )
            {
                int i = htmlcnt.IndexOf("<table", nth+1);
                if (i < 0) break;
                htmltable = htmlcnt.Substring(i, htmlcnt.IndexOf("</table>",i+1) - i + 8);
                htmltable = htmltable.Replace("\r","").Replace("\n","");
                tables.Add(htmltable);
                nth = i;
            }

            return tables;

        }


        private static string place_tables(string btreml, List<string> htmltbl)
        {

            int strtindx = btreml.IndexOf("<body>")+6;

            foreach (string tmp in htmltbl) {

                string regextr = "<tr(.|\\s)*?>((.|\\s)*?)<\\/tr>";
                string regextd = "<td(.|\\s)*?>((.|\\s)*?)<\\/td>";
                MatchCollection matchestr = Regex.Matches(tmp, regextr, RegexOptions.Multiline); 
                MatchCollection matchestd = Regex.Matches(tmp, regextd, RegexOptions.Multiline);
                
                //one-dimension table, do not need to process
                if (matchestr.Count == matchestd.Count) 
                    continue;

                Match match = new Regex("<td.*?>((.|\\s)*?)<\\/td>",RegexOptions.Multiline).Match(tmp);

                string strtvl = match.Groups[0].Value;
                strtvl = strtvl.Replace("\r", "").Replace("\n", "").Replace("  ", "").Replace("&nbsp;", " ").Replace("&quot;", "\"");
                strtvl = new Regex("(<(?!a href|\\/a).*?>)").Replace(strtvl, "");
                //strtvl = strtvl.Replace("\r","").Replace
            
                if ( String.IsNullOrWhiteSpace(strtvl) ) 
                {
                    while (match.Success)
                    {
                        strtvl = match.Groups[0].Value;
                        strtvl = strtvl.Replace("\r", "").Replace("\n", "").Replace("  ", "").Replace("&nbsp;", " ").Replace("&quot;", "\"");
                        strtvl = new Regex("(<(?!a href|\\/a).*?>)").Replace(strtvl, "");
                        if ( !String.IsNullOrWhiteSpace(strtvl) ) break;
                        match = match.NextMatch();
                    }
                }
            
                string endvl = "";
                match = match.NextMatch();

                while (match.Success) {

                    string tmpstr = match.Groups[0].Value;
                    tmpstr = tmpstr.Replace("\r", "").Replace("\n", "").Replace("  ", "").Replace("&nbsp;", " ").Replace("&quot;", "\"");
                    tmpstr = new Regex("(<(?!a href|\\/a).*?>)").Replace(tmpstr, "");
                    if (!String.IsNullOrWhiteSpace(tmpstr)) 
                        endvl = tmpstr;
                    match = match.NextMatch();
                }

                endvl = new Regex("(<(?!a href|\\/a).*?>)").Replace(endvl, "");
                
                //they can contain UTF symbols encoded in HTML. 
                //It's not enough cause - != – for exaple so index of strtvl in btreml will not be found
                strtvl = HttpUtility.HtmlDecode(strtvl);
                strtvl = strtvl.TrimStart(' ');
                endvl = HttpUtility.HtmlDecode(endvl);
                endvl = endvl.TrimStart(' ');

                int i = btreml.IndexOf(new Regex("<a href.*?>|</a>").Replace(strtvl, ""));
                int i2 = btreml.IndexOf(new Regex("<a href.*?>|</a>").Replace(endvl, ""));

                while (i2 > strtindx && i > (i2 - 8))
                {
                    
                    i2 = btreml.IndexOf(new Regex("<a href.*?>|</a>").Replace(endvl, ""), i2 + 1);
                }

                if (i < strtindx || i2 < strtindx || i > (i2 - 8))
                {

                    Console.WriteLine("Html table parsing error");
                    btreml += "<br>\r\n";
                    btreml = btreml.Insert(btreml.Length, tmp.Replace(">", ">\r\n"));
                }
                else
                {
                    btreml = btreml.Remove(i, i2 + new Regex("<a href.*?>|</a>").Replace(endvl, "").Length - i);
                    btreml = btreml.Insert(i, tmp.Replace(">", ">\r\n"));
                }
            }

            return btreml;
        }


        /// <summary>
        /// Tries to find the attachments encoded as Base64 in the .eml file.
        /// </summary>
        /// <returns>The list of attachments found.</returns>
        private IEnumerable<Attachment> GetAttachments()
        {

            IList<Attachment> attachments = new List<Attachment>();

            // Check if we have a valid boundary
            //can also check for multipart
            // can have more than one boundary
            if(Boundary == null)
                return attachments;

            if (Content.Contains("filename*=")) {

                Console.WriteLine("RFC 5987 format is not supported.");
                Environment.Exit(-2);
            }

            if (Content.Contains("Apple-Mail"))
            {

                Console.WriteLine("Apple Mail is not supported.");
                Environment.Exit(-6);
            }

            /*
             * Just fucking great. It can be charset=*; or charset="" (wo ;) or hell knows how else
             * how the hell it's STANDART? die mime die
             */
        
            string emlcharset = null;

            // Split email content on boundary
            //string[] parts = Content.Split(new[] { Boundary }, StringSplitOptions.RemoveEmptyEntries);
            // we can have more than one boundaries so let's split by all of them
            string[] parts = Content.Split(boundaries, StringSplitOptions.RemoveEmptyEntries);

            // Parse each part
            foreach (var part in parts)
            {

                //it's not attachment so we do not need it
                // Good news everyone: some servers can just ignore "filename" and set only "name"
                if (!part.Contains("filename=") && !part.Contains("name=") ) 
                    continue;
                
                // Split on two new line chars to distinguish header and content
                string[] headerAndContent = part.Trim(new[] { '\r', '\n', '-', ' ', '\t' })
                    .Split(new[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.None);

                // Not a valid split
                // Not ever works correctly, needs testing and correcting
                /*
                 * fun fact:
                 * sometimes it's more than 2 strings, like 216 or so, 
                 * they also can contain non-base64 symbols 
                 * and be so long it's impossible to work with them 
                 * (everything just hangs or outofmemory.exception)
                 * I'm not sure why it's happening and how to manage it
                 */
                if (headerAndContent.Length != 2)
                {
                    //fast ugly workaround
                    if (headerAndContent.Length > 2)
                    {

                       Console.WriteLine("Attachment struct issue (possibly redundant CRLF)");
                        //like a SWEABORG
                        //if (part.IndexOf("==") != -1)
                        //{
                            var tmp = part.IndexOf(headerAndContent[1]);
                            //var tmp2 = (part.IndexOf("==\r\n") + 2) - tmp;
                            headerAndContent[1] = part.Substring(tmp, (part.Length - tmp));
                            Array.Resize(ref headerAndContent, 2);
                            GC.Collect();
                        //}
                        //THEREIFIXEDIT. Now it's slow like a slowpoke.
                    }
                    else
                    {
                        continue;
                    }
                }

                // Valid header and content
                string header = headerAndContent[0];
                string content = headerAndContent[1];

                // Look for a valid file name string
                // With UTF-8 Q it can be more than one string
                // UPDATE GREAT WE CAN HAVE \R\N AFTER =
                // name="=?UTF-8?b?0KPRgdGC0LDQsg==?= 2012.pdf" Content-Disposition: attachment;

                /*
                 * also need to add filename*= according to rfc5987
                 */

                Regex regex = new Regex("(?<=filename=)(.|\n|\t){0,3}\"((.|\n|\t)*?)\"");
                Match match = regex.Match(header);
                if (!match.Success)
                {
                    //idiotic mime, idiotic server, something else, but they decided to not add "filename"
                    if (header.Contains("attachment"))
                    {

                        Regex flnmaltrgx = new Regex("(?<=name=)(.|\n|\t){0,3}\"((.|\n|\t)*?)\"");
                        match = flnmaltrgx.Match(header);
                        if (!match.Success)
                        {
                            continue;
                        }
                    }
                    else
                        continue;
                }

                //these are just whistles&bells, ignore them
                if (header.Contains("Content-Disposition: inline"))
                {
                    if (!xtrwhsls_inner) 
                        continue;
                }

                if (header.Contains("quoted-printable"))
                {

                    Console.WriteLine("QP encoded attachment – not supported.");
                    //attstart = 0;
                    continue;
                }

                if (attstart == 0)
                    attstart = Content.IndexOf(part);

                
                /*******
                 * Now we can have strings encoded in Base64 ("?b|B?") or in printable UTF8 ("?Q|q?")
                 * In first case we must decode it, in second – just other decode, simple as bozon catching. 
                 * But UTF names can broke if they are long and then multiline.
                 * To avoid this, first make one big string name.
                 * 
                 * UPDATE: there can be more than one type (Q or B) in one string AND ALSO NON ENCODED NOT MARKED ASCII SYMBOLS. Fuck this, MIME really should die.
                 ******/

                string fileNameStr = match.Groups[2].Value;
                string fileName = null;
                string[] fileNameStrArr = null;

                //detect current encoding
                var rgxchrst = new Regex(@"=\?(.*?)\?");
                Match rgxchrmtch = rgxchrst.Match(fileNameStr);
                
                if (rgxchrmtch.Success)
                    emlcharset = rgxchrmtch.Groups[1].Value;
                

                if (emlcharset != null)
                
                {
                    //in filename we can have ==, ==?=, blank space and other cool stuff
                    // it gets more and more greater. When B, we can have ==?= so how dahell we distinguish between encoding and no-encoding?
                    fileNameStrArr = fileNameStr.Split(new string[] { "\"=?", " =?", " ", "\t=?" }, StringSplitOptions.RemoveEmptyEntries);
                    string multistr = null;
                    foreach (string tmpstr3 in fileNameStrArr)
                    {
                        bool whspbtw = false;
                        
                        if ( fileNameStr.IndexOf(" ", fileNameStr.IndexOf(tmpstr3)) == (fileNameStr.IndexOf(tmpstr3) + tmpstr3.Length) )
                            whspbtw = true;

                        if (tmpstr3.IndexOf("?Q?", 0, StringComparison.OrdinalIgnoreCase) != -1)
                        {

                            if (tmpstr3.IndexOf("\r\n") != -1 || multistr != "")
                            {
                                multistr += Regex.Match(tmpstr3.Substring(tmpstr3.IndexOf("Q?", StringComparison.OrdinalIgnoreCase)), "(?<=\\?)(.*?)(?=\\?)");       
                            }
                            else
                            {
                                System.Net.Mail.Attachment attdecode = System.Net.Mail.Attachment.CreateAttachmentFromString("", tmpstr3);
                                fileName += attdecode.Name;
                            }
                            
                        }
                        
                        if (tmpstr3.IndexOf("?B?", 0, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            if (multistr != null)
                            {
                                multistr = multistr.Insert(0, "=?" + emlcharset + "?Q?");
                                multistr = multistr.Insert(multistr.Length, "?=");
                                System.Net.Mail.Attachment attdecode = System.Net.Mail.Attachment.CreateAttachmentFromString("", multistr);
                                fileName += attdecode.Name;
                                multistr = null;
                            }
                            if (tmpstr3 == "") continue;
                            byte[] fileNameRaw = null;
                            string tmpstr5 = null;
                            //everything we need is between two ?
                            tmpstr5 += Regex.Match(tmpstr3.Substring(tmpstr3.IndexOf("B?", StringComparison.OrdinalIgnoreCase)), "(?<=\\?)(.*?)(?=\\?)");
                            
                            /*
                            //LAKA SWEABOROUGH FAIR MCU STYLAH NEVAH DIAS
                            for (var i = ((tmpstr5.Length / 4) * 3) % 4; i > 0; i--) {
                                tmpstr5 += "=";
                            }
                            */

                            fileNameRaw = System.Convert.FromBase64String(tmpstr5);
                            fileName += System.Text.Encoding.GetEncoding(emlcharset).GetString(fileNameRaw);
                        }

                        if (tmpstr3.IndexOf("?Q?", 0, StringComparison.OrdinalIgnoreCase) == -1 && tmpstr3.IndexOf("?B?", 0, StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            if (multistr != null)
                            {
                                multistr = multistr.Insert(0, "=?"+emlcharset+"?Q?");
                                multistr = multistr.Insert(multistr.Length, "?=");
                                System.Net.Mail.Attachment attdecode = System.Net.Mail.Attachment.CreateAttachmentFromString("", multistr);
                                fileName += attdecode.Name;
                                multistr = null;
                            }
                            
                            //everything's was ok till now so it's possibly tail of shitty mime B64 formatting
                            fileName += tmpstr3.Replace("=", null);
                        }

                        if (whspbtw) fileName += "_";

                    }
                    if (multistr != null)
                    {
                        multistr = multistr.Insert(0, "=?"+emlcharset+"?Q?");
                        multistr = multistr.Insert(multistr.Length, "?=");
                        System.Net.Mail.Attachment attdecode = System.Net.Mail.Attachment.CreateAttachmentFromString("", multistr);
                        fileName += attdecode.Name;
                        multistr = null;
                    }
                }
                //assume that we have no encoding, just ASCII
                else
                {
                    fileName = fileNameStr;
                }

                fileName = fileName.Replace("  ", "_").Replace(" ", "_").Replace("__","_");
                fileName = fileName.Replace("_. ", ".");
                fileName = fileName.Trim('_');

                if (fileName.Contains(".html") || fileName.Contains(".htm") || fileName.Contains(".xml") ) //
                {
                    //attstart = 0;
                    continue;
                }

                if (fileName.IndexOf('=') != -1)
                {

                    Console.WriteLine("filename reading error (filename still containing '=')");
                    Environment.Exit(2);
                }

                //string fileName = System.Text.Encoding.GetEncoding(20866).GetString(fileNameRaw);
                //I do not know how, but filename in Windows FS got right encoding without converting
                content = content.Replace("-", null);
                foreach (string tmpbndr in boundaries) {

                    content = content.Replace(tmpbndr, null);
                }
                GC.Collect();

                if (Regex.IsMatch(content, @"^[a-zA-Z0-9\+\/]*={0,2}$"))
                {        
                     Console.WriteLine("Base64 content error");
                     //attstart = 0;
                     continue;
                }
                

                byte[] raw;

                try
                {
                    // Try to convert the Base64 content to bytes
                    raw = Convert.FromBase64String(content);
                }
                catch (Exception)
                {
                    Console.WriteLine("FromBase64String content decoding error");
                    //attstart = 0;
                    continue;
                }

                // Successful conversion, add attachment to the list

                attachments.Add(new Attachment(fileName, raw));
              
            }

            //// Return all attachments found
            return attachments;
        }

        /// <summary>
        /// Saves all attachments of this email to the output directory.
        /// </summary>
        /// <param name="outputDirectory">The output directory.</param>
        /// <returns>The number of files saved.</returns>
        public int SaveAttachments(string outputDirectory, string bckppth, bool sort, string ftppth, bool xtrwhsls)
        {
            // Keep track of total number attachments
            int count = 0;
            xtrwhsls_inner = xtrwhsls;

            List<string> patches = new List<string> { };
            string[] tmpdate = new string[2];
            string path = "";
            string tmpoutdir = outputDirectory;

            bool crdir = false;
                       
            // Extract each attachment
            foreach (var attachment in Attachments)
            {
                // Write bytes to output file
                if (!crdir && Attachments.Count() > 0 && sort) {

                    tmpdate = getdateMyyyy();
                    string tmppth = outputDirectory + "\\" + tmpdate[0] + "\\" + tmpdate[1];
                    if (!Directory.Exists(tmppth))
                        Directory.CreateDirectory(tmppth);
                    outputDirectory += "\\" + tmpdate[0] + "\\" + tmpdate[1];
                    crdir = true;
                }

                try
                {
                    path = Path.Combine(outputDirectory, attachment.FileName);
                }
                catch {
                    Console.WriteLine("Path problem, assistance needed.");
                    Environment.Exit(-9);
                }

                //if two emails have identical flnms, we'll just overwrite it, that's not cool
                if (File.Exists(path))
                {
                    FileInfo f = new FileInfo(path);
                    if (f.Length == attachment.Content.Length) { 
                        
                        Console.WriteLine("Older attachment file found. No replacement done.");
                        if (attstart > 0) patches.Add(path.ToString());
                    }
                    else
                    {
                        //maybe while (file exist) ?
                        string tmpflnm = attachment.FileName.Insert(attachment.FileName.LastIndexOf("."), "_dfn_" + GetRandomAlphaNumeric());
                        path = null;

                        path = Path.Combine(outputDirectory, tmpflnm);
                        if (File.Exists(path)) {
                            Console.WriteLine("Atts duplicate file names, assistance needed.");
                            Environment.Exit(8);
                        }
                        if (attstart > 0) patches.Add(path.ToString());
                        File.WriteAllBytes(path, attachment.Content);
                    }

                }
                else
                {
                    if (attstart > 0) patches.Add(path.ToString());
                    File.WriteAllBytes(path, attachment.Content);
                }
                count++;

            }


            //and yes, I know, this code looks like shit. Still, it works.
            if (attstart > 0 && bckppth != "" && count > 0) //
            {
                
                File.Copy(FileName, Path.Combine(bckppth,Path.GetFileName(FileName)));

                string tmpchrst = gettxtcharset();
                string outchrst = "utf-8";
                bool alconv = false;
                
                /*
                 * Well, this is great.
                 * Just figured out that Content-type can be before from, to, subj...
                 * Why not, really, this standart is completely ruined already,
                 * let's put anything anywhere.
                 */

                string[] parts = Content.Substring(0, attstart).Split(boundaries, StringSplitOptions.RemoveEmptyEntries);

                if ( parts.Any(s => s.Contains("binary")) || parts.Any(s => s.Contains("8bit")) )
                {
                    string tmp = File.ReadAllText(FileName, Encoding.GetEncoding(tmpchrst));
                    alconv = true;
                    tmp = tmp.Substring(0, attstart);
                    parts = tmp.Split(boundaries, StringSplitOptions.RemoveEmptyEntries);
                }

                string bettereml = parts[0];

                if (!parts[1].Contains("boundary=") && !parts[1].Contains("Content-Type:")) {

                    bettereml += parts[1];
                }

                string[] mimekeystoremove = { "Content-Type:.*", "boundary=.*", "X-(?!Envelope).*", "charset=.*", "Content-Transfer-Encoding:.*", "This is a multi-part message in MIME format.*" };

                foreach ( string fltr in mimekeystoremove ) {

                    Regex rgx = new Regex(fltr); 
                    bettereml = rgx.Replace(bettereml, "");
                }

                Regex rgx100 = new Regex("[\r\n]{1,}\t{0,1}[\r\n]{1,}");
                bettereml = rgx100.Replace(bettereml, "\r\n");

                bettereml = bettereml.Trim(new[] { '\r', '\n', '-', ' ', '\t' });
                bettereml += "\r\nContent-Type: text/html; charset=UTF-8\r\nContent-Transfer-Encoding: binary\r\n\r\n";

                if ( parts.Any(s => s.Contains("text/plain")) )
                {
                    
                    int i = Array.FindIndex(parts, tmp => tmp.Contains("text/plain"));
                    string tmpeml = parts[i];
                    string trnenc = gettxttransferenc(tmpeml);

                    bettereml += "<html><head><meta charset=\"UTF-8\" /></head><body>\r\n";

                    //just plain text, maybe
                    if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) == -1 && trnenc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        //exclude tech
                        Regex rgx = new Regex("Content-Type:.*|charset=.*|Content-Transfer-Encoding:.*");
                        tmpeml = rgx.Replace(tmpeml, "");
                        if (tmpeml == "")
                        {
                            bettereml += "\r\n--\r\n";
                        }
                        else
                        {
                            if (!alconv)
                            {
                                tmpeml = convstr(tmpeml, tmpchrst, outchrst);
                            }
                            tmpeml = tmpeml.Replace("\r\n", "<br>");
                            bettereml += tmpeml;
                        }
                    }
                    else
                    {
                        if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            tmpeml = tmpeml.Substring(tmpeml.IndexOf("\r\n\r\n") + 4, tmpeml.Length - tmpeml.IndexOf("\r\n\r\n") - 4);
                            tmpeml = DecodeQuotedPrintable(tmpeml, tmpchrst);
                            Regex rgx = new Regex("charset=.*?>");
                            tmpeml = rgx.Replace(tmpeml, "charset=\"UTF-8\">");
                            Regex rgx2 = new Regex("(\r\n){3,}");
                            tmpeml = rgx2.Replace(tmpeml, "\r\n\r\n");
                            tmpeml = tmpeml.Replace("\r\n", "<br>");
                            bettereml += tmpeml;
                        }
                        if (trnenc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            tmpeml = tmpeml.Substring(tmpeml.IndexOf("\r\n\r\n")+4, tmpeml.Length - tmpeml.IndexOf("\r\n\r\n")-4);
                            tmpeml = tmpeml.Replace("\r\n", null).Replace("--", null);
                            tmpeml = System.Text.Encoding.GetEncoding(tmpchrst).GetString(System.Convert.FromBase64String(tmpeml));
                            Regex rgx = new Regex("charset=.*?>");
                            tmpeml = rgx.Replace(tmpeml, "charset=\"UTF-8\">");
                            tmpeml = tmpeml.Replace("\r\n", "<br>").Replace("\n", "<br>");
                            bettereml += tmpeml;
                        }
                    }
 
                    //add here search for tables
                    //omg when its gonna end

                    if (parts.Any(s => s.Contains("text/html")))
                    {
                        List <string> tmp = get_tables(parts[Array.FindIndex(parts, tmp2 => tmp2.Contains("text/html"))], tmpchrst);
                        if (tmp != null)
                            bettereml = place_tables(bettereml, tmp);
                    }

                }
                else {

                    if (parts.Any(s => s.Contains("text/html")))
                    {

                        int i = Array.FindIndex(parts, tmp => tmp.Contains("text/html"));
                        string tmpeml = parts[i];

                        string trnenc = gettxttransferenc(tmpeml);

                        bettereml += "<html><head><meta charset=\"UTF-8\" /></head><body>\r\n";

                        if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) == -1 && trnenc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) == -1)
                        {

                            //just plain text, maybe
                            Regex rgx = new Regex("Content-Type:.*|charset=.*|Content-Transfer-Encoding:.*");
                            tmpeml = rgx.Replace(tmpeml, "");
                            if (tmpeml == "")
                            {
                                bettereml += "\r\n--\r\n";
                            }
                            else
                            {
                                tmpeml = convstr(tmpeml, tmpchrst, outchrst);
                                tmpeml = tmpeml.Replace("\r\n", "<br>");
                                bettereml += tmpeml;
                            }
                        }
                        else
                        {

                            if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                tmpeml = tmpeml.Substring(tmpeml.IndexOf("\r\n\r\n") + 4, tmpeml.Length - tmpeml.IndexOf("\r\n\r\n") - 4);
                                tmpeml = tmpeml.Replace("\r\n", null).Replace("--", null);
                                tmpeml = DecodeQuotedPrintable(tmpeml, tmpchrst);
                                tmpeml = convstr(tmpeml, tmpchrst, outchrst);
                                Regex rgx = new Regex("charset=.*?>");
                                tmpeml = rgx.Replace(tmpeml, "charset=\"UTF-8\">");
                                tmpeml = tmpeml.Replace("\r\n", "<br>");
                                bettereml += tmpeml;
                            }
                            if (trnenc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                tmpeml = tmpeml.Substring(tmpeml.IndexOf("\r\n\r\n") + 4, tmpeml.Length - tmpeml.IndexOf("\r\n\r\n") - 4);
                                tmpeml = tmpeml.Replace("\r\n", null).Replace("--", null);
                                tmpeml = System.Text.Encoding.GetEncoding(tmpchrst).GetString(System.Convert.FromBase64String(tmpeml));
                                Regex rgx = new Regex("charset=.*?>");
                                tmpeml = rgx.Replace(tmpeml, "charset=\"UTF-8\">");
                                tmpeml = tmpeml.Replace("\r\n", "<br>");
                                bettereml += tmpeml;
                            }
                        }
                    }
                    
                    //eml body contains no text at all. But we need html to build our atts download list.
                    else {

                        bettereml += "<html><head><meta charset=\"UTF-8\" /></head><body>\r\n";
                    }

                }

                bettereml.Replace("</html>",null).Replace("</body>",null);

                string tmpcnt2 = "<br><h3>Вложения:</h3>";
                foreach (string pth in patches)
                {
                    tmpcnt2 += "<b>" + pth.Substring(pth.LastIndexOf("\\") + 1) + "</b><br>";
                    tmpcnt2 += " В офисе: ";
                    tmpcnt2 += "<a href=\"file:///" + pth + "\">открыть файл</a> ";
                    tmpcnt2 += "<a href=\"file:///" + pth.Substring(0, pth.LastIndexOf("\\")) + "\">открыть папку</a><br>";
                    if (ftppth != ""){
                        ftppth = ftppth.Trim('\"');
                        tmpcnt2 += "Вне офиса: <a href=\"ftp://" + ftppth + "/";
                        if (sort)
                            tmpcnt2 += tmpdate[0] + "/" + tmpdate[1] + "/";
                        tmpcnt2 += HttpUtility.UrlEncode(pth.Substring(pth.LastIndexOf("\\")+1)) + "\">скачать</a><br><br>";
                    }
                }

                string tmpstr = bettereml.Substring(bettereml.IndexOf("<html>"));

                if (tmpstr.Contains("From:") || tmpstr.Contains("---"))
                {
                    int i = 0;
                    if (tmpstr.IndexOf("---") == -1)
                    {
                        i = tmpstr.IndexOf("From:"); 
                    }
                    else {
                        i = tmpstr.IndexOf("---");
                    }
                    i += bettereml.Substring(0, bettereml.IndexOf("<html>")).Length; 
                    bettereml = bettereml.Insert(i, "<hr/>"); 
                    //i -= 5;
                    bettereml = bettereml.Insert(i, tmpcnt2);
                    
                }
                else
                {
                    bettereml += tmpcnt2;
                }
                bettereml +="<br></body></html>";
                bettereml = bettereml.Replace("<br>", "<br>\r\n");

                Match m = Regex.Match(bettereml, "(^(\\s{0,})<br>(\r\n|\n)){2,}", RegexOptions.Multiline);
                while (m.Success)
                {
                    bettereml = bettereml.Replace("\r\n"+(m.Groups[0].Value), "\r\n<br>\r\n");
                    m = m.NextMatch();
                }

                FileInfo f = new FileInfo(FileName);
                string tmppth = f.DirectoryName;
                tmppth = Path.Combine(tmppth, FileName);
                File.Delete(tmppth);
                File.WriteAllText(tmppth, bettereml);
            }

            // Return count
            return count;
        }
    }
}
