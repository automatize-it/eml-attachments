# eml-attachments

This utility extracts attachments from .eml MIME files in batch mode.

Fork erikvdv1/eml-attachments respectively.
Added many improvements in handling:
- encodings 
- MIME idiotic formatting


Usage: 
eml-attachments -i path/to/file.eml (-o output/directory (--deletefromorigin YES -b backup/path) --sortbyMyyyy --ftp "ftpserver:port/path")
-i input file (required);

-o output directory (optional);
--deletefromorigin YES: DELETES ORIGINAL MESSAGE AND REPLACES IT BY SIMPLE TEXT/HTML COPY IN UTF-8, with hyperlinks to attachment files. At you own risk! (optional);
-b backup path: saves original message to given dir; required with --deletefromorigin;
--sortbyMyyyy: creates yyyy folder in output folder then 3-letter month folder then places .eml there (optional);
--ftp: ftp link to attachment file, wo "ftp://" (optional);




Uses .NET 4.0

Looks like it's the only one utility for Windows that extracts attachments correctly.
