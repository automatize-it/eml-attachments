﻿#region License
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


namespace Infiks.Email
{
        
    /// <summary>
    /// A class for representing an email (eml file).
    /// </summary>
    class Email
    {
        /// <summary>
        /// The original file name of the .eml file.
        /// </summary>
        
        int attstart = 0;
        public string FileName { private set; get; }
        public string[] boundaries = new string[1];

        /// <summary>
        /// The boundary string of the email.
        /// </summary>
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

        private string gettxttransferenc(string tmppart)
        {

            Regex findchrstrgx = new Regex("Content-transfer-encoding: \"{0,1}(.*?)[\"\\;\\s\n]", RegexOptions.IgnoreCase);

            Match trnfenctmth = findchrstrgx.Match(tmppart);
            string trnfenc = null;
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
            var occurences = new Regex(@"(=[0-9A-Z]{2})", RegexOptions.Multiline); //{1,}
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
                    input = input.Replace(match.Groups[0].Value, new String(hexChar));
                    
                    
                    GC.Collect();
                }
                catch
                { Console.WriteLine("QP dec err"); }
            }
            input = input.Replace("?=", ""); //.Replace("\r\n", "");

            return input;
        }

        private string convstr(string origstr, string inchrst, string outchrst)
        {
            
            //Encoding unicode = Encoding.Unicode;

            Encoding destenc = Encoding.GetEncoding(outchrst);

            Encoding currenc = Encoding.GetEncoding(inchrst);

            //currenc = Encoding.GetEncoding(65001);

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

            Regex finddatergx = new Regex("Date:(.*)");
            Match datemth = finddatergx.Match(Content);

            string msgmonthyear = "";

            if (!datemth.Success)
            {
                Console.WriteLine("Date error");
            }

            //split by " ", elements index 2 and 3
            msgmonthyear = datemth.Groups[1].Value;
            string[] strarr = msgmonthyear.Split(' ');

            return strarr;
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

            /*
             * Just fucking great. It can be charset=*; or charset="" (wo ;) or hell knows how else
             * how the hell it's STANDART? die mime die
             */

            //Regex findchrstrgx = new Regex("charset=\"{0,1}(.*?)[\"\\;\\s\n]");
            //Match emlcharsetmth = findchrstrgx.Match(Content);
            string emlcharset = null;
            //if (emlcharsetmth.Success)
            //{
            //   emlcharset = emlcharsetmth.Groups[1].Value;
            //}

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
                //content.Replace("-", null);
                //GC.Collect();

                // Look for a valid file name string
                // With UTF-8 Q it can be more than one string
                // UPDATE GREAT WE CAN HAVE \R\N AFTER =
                // name="=?UTF-8?b?0KPRgdGC0LDQsg==?= 2012.pdf" Content-Disposition: attachment;
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
                    continue;

                if (attstart == 0) 
                    attstart = Content.IndexOf(part);

                //string fileName = "test1";
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
                {
                    emlcharset = rgxchrmtch.Groups[1].Value;
                }

                //if (fileNameStr.IndexOf("=?" + emlcharset + "?", StringComparison.OrdinalIgnoreCase) != -1 )
                if (emlcharset != null)
                
                {
                    //in filename we can have ==, ==?=, blank space and other cool stuff
                    // it gets more and more greater. When B, we can have ==?= so how dahell we distinguish between encoding and no-encoding?
                    fileNameStrArr = fileNameStr.Split(new string[] { "\"=?", " =?", " ", "\t=?" }, StringSplitOptions.RemoveEmptyEntries);
                    string multistr = null;
                    foreach (string tmpstr3 in fileNameStrArr)
                    {

                        if (tmpstr3.IndexOf("?Q?", 0, StringComparison.OrdinalIgnoreCase) != -1)
                        {

                            if (tmpstr3.IndexOf("\r\n") != -1 || multistr != "")
                            {
                                //string[] reparr = { emlcharset, "?Q?", "?=", "=?", "\";", " " };
                                multistr += Regex.Match(tmpstr3.Substring(tmpstr3.IndexOf("Q?", StringComparison.OrdinalIgnoreCase)), "(?<=\\?)(.*?)(?=\\?)");

                                //multistr += tmpstr3.ReplArrIgnoreCase(reparr, null);
                                    
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

                fileName = fileName.Replace(" ", "_");
                fileName = fileName.Replace("_. ", ".");
                fileName = fileName.Trim('_');
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
        public int SaveAttachments(string outputDirectory, string bckppth, bool sort)
        {
            // Keep track of total number attachments
            int count = 0;

            List<string> patches = new List<string> { };
            string[] tmpdate = new string[2];
            string path = "";
            string tmpoutdir = outputDirectory;

            if (sort)
            {
                //year
                tmpdate[0] = (getdateMyyyy())[4];
                //month
                tmpdate[1] = (getdateMyyyy())[3];

                string tmppth = outputDirectory + "\\" + tmpdate[0] + "\\" + tmpdate[1];
                if (!Directory.Exists(tmppth)) Directory.CreateDirectory(tmppth);
                outputDirectory += "\\" + tmpdate[0] + "\\" + tmpdate[1];
            }


            // Extract each attachment
            foreach (var attachment in Attachments)
            {
                // Write bytes to output file
                uint dfn = 1;

                path = Path.Combine(outputDirectory, attachment.FileName);

                if (File.Exists(path))
                {

                    string tmpflnm = attachment.FileName.Insert(attachment.FileName.LastIndexOf("."), "_dfn" + (dfn++));
                    path = null;
                    //if (sort) outputDirectory += "\\" + tmpdate[0] + "\\" + tmpdate[1];
                    path = Path.Combine(outputDirectory, tmpflnm);
                    if (attstart > 0) patches.Add(path.ToString());
                    File.WriteAllBytes(path, attachment.Content);

                }
                else
                {
                    if (attstart > 0) patches.Add(path.ToString());
                    File.WriteAllBytes(path, attachment.Content);
                }
                count++;

            }


            //and yes, I know, this code looks like shit. Still, it works.
            if (attstart > 0 && bckppth != "") //
            {
                File.Copy(FileName, Path.Combine(bckppth,FileName));

                string tmpchrst = gettxtcharset();
                string outchrst = "utf-8";
                
                /*
                 * Well, this is great.
                 * Just figured out that Content-type can be before from, to, subj...
                 * Why not, really, this standart is completely ruined already,
                 * let's put anything anywhere.
                 */
                string[] parts = Content.Substring(0, attstart).Split(boundaries, StringSplitOptions.RemoveEmptyEntries);

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

                //string bettereml = Content.Substring(0, Content.IndexOf("Content-Type:"));
                
                bettereml = bettereml.Trim(new[] { '\r', '\n', '-', ' ', '\t' });
                bettereml += "\r\nContent-Type: text/html; charset=UTF-8\r\nContent-Transfer-Encoding: binary\r\n\r\n";
                //string tmpcnt = Content.Substring(0, attstart); 

                
                //string tmpcnt = Content.Substring(Content.IndexOf("boundary", attstart); 

                //if ( parts.Contains("text/plain") )
                if ( parts.Any(s => s.Contains("text/plain")) )
                {
                    
                    int i = Array.FindIndex(parts, tmp => tmp.Contains("text/plain"));
                    string tmpeml = parts[i];

                    string trnenc = gettxttransferenc(tmpeml);
                    
                    bettereml += "<html><head><meta charset=\"UTF-8\" /></head><body>";

                    if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) == -1 && trnenc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        //just plain text, maybe
                        tmpeml = convstr(tmpeml, tmpchrst, outchrst);
                        //bettereml += EncodeQuotedPrintable(tmpeml, outchrst);
                        //tmpeml = Regex.Replace(tmpeml, ".{76}(?!$)", "$0<br>");
                        bettereml += tmpeml;
                    }
                    else
                    {
                        if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            tmpeml = tmpeml.Substring(tmpeml.IndexOf("\r\n\r\n")+4, tmpeml.Length - tmpeml.IndexOf("\r\n\r\n")-4);
                            
                            tmpeml = DecodeQuotedPrintable(tmpeml, tmpchrst);
                            
                            Regex rgx = new Regex("charset=.*?>");
                            tmpeml = rgx.Replace(tmpeml, "charset=3D\"UTF-8\">");
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
                            tmpeml = tmpeml.Replace("\r\n", "<br>");
                            bettereml += tmpeml;
                        }
                    } 
                    
                    //bettereml += convstr(parts[i], tmpchrst, outchrst);
                    //bettereml += "</pre>";
                }
                else {
                    
                    if ( parts.Any(s => s.Contains("text/html"))) 
                    {
                    
                        int i = Array.FindIndex(parts, tmp => tmp.Contains("text/html"));
                        string tmpeml = parts[i];

                        string trnenc = gettxttransferenc(tmpeml);

                        bettereml += "<html><head><meta charset=\"UTF-8\" /></head><body>";

                        if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) == -1 && trnenc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) == -1)
                        {

                            //just plain text, maybe
                            tmpeml = convstr(tmpeml, tmpchrst, outchrst);
                            tmpeml = tmpeml.Replace("\r\n", "<br>");
                            bettereml += tmpeml;
                        }
                        else
                        {

                            if (trnenc.IndexOf("quoted-printable", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                tmpeml = DecodeQuotedPrintable(tmpeml, tmpchrst);
                                tmpeml = convstr(tmpeml, tmpchrst, outchrst);
                                Regex rgx = new Regex("charset=.*?>");
                                tmpeml = rgx.Replace(tmpeml, "charset=3D\"UTF-8\">");
                                tmpeml = tmpeml.Replace("\r\n", "<br>");
                                bettereml += tmpeml;
                            }
                            if (trnenc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                tmpeml = System.Text.Encoding.GetEncoding(tmpchrst).GetString(System.Convert.FromBase64String(tmpeml));
                                Regex rgx = new Regex("charset=.*?>");
                                tmpeml = rgx.Replace(tmpeml, "charset=\"UTF-8\">");
                                tmpeml = convstr(tmpeml, tmpchrst, outchrst);
                                //bettereml += EncodeQuotedPrintable(tmpeml, tmpchrst);
                                tmpeml = tmpeml.Replace("\r\n", "<br>");
                                //tmpeml = Regex.Replace(tmpeml, ".{76}(?!$)", "$0<br>");
                                bettereml += tmpeml;
                            }
                        }         
                    }
                }

                bettereml.Replace("</html>",null).Replace("</body>",null);

                string tmpcnt2 = "<br><br>Вложения:<br><br>";
                foreach (string pth in patches)
                {
                    tmpcnt2 += "<b>" + pth.Substring(pth.LastIndexOf("\\") + 1) + "</b><br>";
                    tmpcnt2 += " В офисе: ";
                    tmpcnt2 += "<a href=\"file:///" + pth + "\">открыть файл</a> ";
                    tmpcnt2 += "<a href=\"file:///" + pth.Substring(0, pth.LastIndexOf("\\")) + "\">открыть папку</a><br>";
                    tmpcnt2 += "Вне офиса: <a href=\"ftp://" + pth + "\">скачать</a><br><br>";
                }

                string tmpstr = bettereml.Substring(bettereml.IndexOf("<html>"));

                if (tmpstr.Contains("From:"))
                {
                    int i = tmpstr.IndexOf("From:") + bettereml.Substring(0, bettereml.IndexOf("<html>")).Length;
                    bettereml = bettereml.Insert(i, tmpcnt2);
                }
                else
                {
                    bettereml += tmpcnt2;
                }
                bettereml +="<br></body></html>";

                /*
                int z = bettereml.IndexOf("html");
                uint cnt = 0;
                while (z < bettereml.Length) {

                    if (cnt == 77) {

                        bettereml = bettereml.Insert(z, "=\r\n");
                        cnt = 0;
                    }
                    z++;
                    cnt++;
                }
                */
 


                File.Delete(FileName);
                string path2 = Path.Combine(tmpoutdir, FileName);
                File.WriteAllText(path2, bettereml);
            }


            // Return count
            return count;
        }
    }
}
