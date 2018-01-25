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
using System.Linq;

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

            /*
             * -i – input file
             * -o – output dir
             * --deletefromorigin – cut attachments from eml file
             * YES – parameter for --deletefromorigin
             * -b – dir to place original file if -deletefromorigin
             */
            string[] argsset = { "-i", "-o", "--deletefromorigin", "YES", "-b", "--sortbyMyyyy" };

            // Check arguments
            if (args.Length == 0 || args[0] == "/?" || (!args.Contains(argsset[0])))
            {
                WriteHelp();
                return;
            }

            string fileName = args[(Array.FindIndex(args, tmp => args.Contains(argsset[0]))) + 1];
            
            string outpath = "";
            string bckppath = "";
            bool sort = false;

            //int i = (Array.FindIndex(args, row => row.Contains(argsset[1])));

            if (args.Contains(argsset[1]))
                outpath = args[(Array.FindIndex(args, tmp => tmp.Contains(argsset[1]))) + 1];

            if (args.Contains(argsset[2]))
            {
                if (args[(Array.FindIndex(args, tmp => tmp.Contains(argsset[2]))) + 1] != argsset[3] || !args.Contains(argsset[4]) )
                {
                    
                    Console.WriteLine("Not sure what you're doing? Then better not.");
                    return;
                }
                bckppath = args[(Array.FindIndex(args, tmp => tmp.Contains(argsset[4]))) + 1];
            }

            if (args.Contains(argsset[5])) sort = true; 
            
            // Check if file exists
            //string fileName = args[0];
            /*
            string outpath = null;
            if (args.Length > 1)
            {
                outpath = args[1];
            }
             */
            if (!File.Exists(fileName))
            {
                Console.WriteLine("Cannot find file: {0}.", fileName);
                return;
            }
             

            // Extract attachments
            Email email = new Email(fileName);
            int count;
            if ( outpath != "" )
            {
                count = email.SaveAttachments(Path.GetFullPath(outpath),bckppath,sort);
            }
            else
            {
                count = email.SaveAttachments(Path.GetDirectoryName(fileName),bckppath,sort);
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
            Console.WriteLine(@"{0} -i path\to\eml\file (-o output\dir) (--deletefromorigin YES -b backup\path) (--sortbyMyyyy)", assemblyName);
        }
        
        
    }
}
