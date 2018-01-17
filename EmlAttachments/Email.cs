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

            Regex findchrstrgx = new Regex("charset=\"{0,1}(.*?)[\"\\;\\s\n]");
            Match emlcharsetmth = findchrstrgx.Match(Content);
            string emlcharset = null;
            if (emlcharsetmth.Success)
            {
                emlcharset = emlcharsetmth.Groups[1].Value;
            }

            // Split email content on boundary
            //string[] parts = Content.Split(new[] { Boundary }, StringSplitOptions.RemoveEmptyEntries);
            // we can have more than one boundaries so let's split by all of them
            string[] parts = Content.Split(boundaries, StringSplitOptions.RemoveEmptyEntries);

            // Parse each part
            foreach (var part in parts)
            {

                //it's not attachment so we do not need it
                if (!part.Contains("filename=")) continue;
                
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
                    //Console.WriteLine("File name not found – possibly incorrect filename field formatting");
                    continue;
                }

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

                if (fileNameStr.IndexOf("=?" + emlcharset + "?", StringComparison.OrdinalIgnoreCase) != -1 )
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
                            //fileName += '_';
                        }
                        //Assume it's ?B? WE FUKEEN CANNOT ELSE
                        if (tmpstr3.IndexOf("?B?", 0, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            if (multistr != null)
                            {
                                multistr = multistr.Insert(0, "=?UTF-8?Q?");
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
                            //fileName += "_";
                        }

                        if (tmpstr3.IndexOf("?Q?", 0, StringComparison.OrdinalIgnoreCase) == -1 && tmpstr3.IndexOf("?B?", 0, StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            if (multistr != null)
                            {
                                multistr = multistr.Insert(0, "=?UTF-8?Q?");
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
                        multistr = multistr.Insert(0, "=?UTF-8?Q?");
                        multistr = multistr.Insert(multistr.Length, "?=");
                        System.Net.Mail.Attachment attdecode = System.Net.Mail.Attachment.CreateAttachmentFromString("", multistr);
                        fileName += attdecode.Name;
                        multistr = null;
                    }
                }
                //No encoding, just ASCII
                else
                {
                    fileName = fileNameStr;
                }

                fileName = fileName.Replace(" ", "_");
                fileName = fileName.Replace("_. ", ".");
                fileName = fileName.Trim('_');
                if (fileName.IndexOf('=') != -1)
                {

                    Console.WriteLine("UTF filename reading error (filename still containing '=')");
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
        public int SaveAttachments(string outputDirectory)
        {
            // Keep track of total number attachments
            int count = 0;

            // Extract each attachment
            foreach (var attachment in Attachments)
            {
                // Write bytes to output file
                string path = Path.Combine(outputDirectory, attachment.FileName);
                File.WriteAllBytes(path, attachment.Content);
                count++;
            }

            // Return count
            return count;
        }
    }
}
