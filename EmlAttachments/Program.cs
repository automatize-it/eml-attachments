#region License
// <copyright file="Program.cs" company="Infiks">
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
using System.IO;

namespace Infiks.Email
{
    /// <summary>
    /// The main class for starting the program.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entrypoint.
        /// </summary>
        /// <param name="args">One argument, the .eml file name.</param>
        static void Main(string[] args)
        {

            // Check arguments
            if (args.Length == 0 || args[0] == "/?")
            {
                WriteHelp();
                return;
            }

            // Check if file exists
            string fileName = args[0];
            string outpath = null;
            if (args.Length > 1)
            {
                outpath = args[1];
            }
            if (!File.Exists(fileName))
            {
                Console.WriteLine("Cannot find file: {0}.", fileName);
                return;
            }

            // Extract attachments
            Email email = new Email(fileName);
            int count;
            if (args.Length > 1)
            {
                count = email.SaveAttachments(Path.GetFullPath(outpath));
            }
            else
            {
                count = email.SaveAttachments(Path.GetDirectoryName(fileName));
            }
            Console.WriteLine("{0} attachments extracted.", count);
        }

        /// <summary>
        /// Writes the help text for commandline users.
        /// </summary>
        private static void WriteHelp()
        {
            String assemblyName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            Console.WriteLine("Extracts the attachments of an .eml file.");
            Console.WriteLine();
            Console.WriteLine(@"{0} drive:\path\to\eml\file (output\dir)", assemblyName);
        }
        
        
    }
}
