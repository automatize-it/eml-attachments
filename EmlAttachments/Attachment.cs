#region License
// <copyright file="Attachment.cs" company="Infiks">
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
namespace Infiks.Email
{
    /// <summary>
    /// A class for representing an attachment.
    /// </summary>
    public class Attachment
    {
        /// <summary>
        /// The file name of the attachment.
        /// </summary>
        public string FileName { private set; get; }

        /// <summary>
        /// The raw contents of the attachment.
        /// </summary>
        public byte[] Content { private set; get; }

        /// <summary>
        /// Creates a new attachment with the specified file name and contents.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="content">The raw content.</param>
        public Attachment(string fileName, byte[] content)
        {
            FileName = fileName;
            Content = content;
        }
    }
}
