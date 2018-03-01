# eml-attachments

This utility extracts attachments from .eml MIME files in batch mode.
Также ниже описание на русском языке.

Fork erikvdv1/eml-attachments respectively.
Added many improvements in handling:
- encodings 
- MIME idiotic formatting


Usage: 
eml-attachments -i path/to/file.eml (-o output/directory (--deletefromorigin YES -b backup/path) --sortbyMyyyy --ftp "ftpserver:port/path")

-i input file (required);

-o output directory (optional);


Next set of keys makes MODIFYING OF ORIGINAL MESSAGE.
It creates simplified utf-8 version of message, deletes attachments from message and inserts links to unpacked attachments: file:///path/to/file file:///path/to/dir and ftp://path/to file with text IN RUSSIAN LANGUAGE. 
Html formatting of original message will be lost. For main mail body, text/plain section will be copied from original.
If original message contains tables, software tries to place it in original structure (from "table to /table"), but this part of software is not working correctly with complicated HTMLs.

--deletefromorigin YES: DELETES ORIGINAL MESSAGE AND REPLACES IT BY SIMPLE TEXT/HTML COPY IN UTF-8, with hyperlinks to attachment files. At you own risk! (optional);

-b backup path: saves original message to given dir; required with --deletefromorigin;

--sortbyMyyyy: creates yyyy folder in output folder then 3-letter month folder then places .eml there (optional);

--ftp: ftp link to attachment file, wo "ftp://" (optional);


Uses .NET 4.0

Looks like it's the only one utility for Windows that extracts attachments correctly.


Утилита для пакетного извлечения вложений из файлов формата eml.
УЧИТЫВАЕТ возможность того, что имена файлов бывают НЕ ТОЛЬКО в кодировке ASCII.
Создаёт КОРРЕКТНЫЕ имена файлов в ОС Windows.

Также утилита может заменять вложения в теле письма ссылками на локальные ресурсы и сервер ftp.
При этом оригинальное письмо БУДЕТ ИЗМЕНЕНО:
- создаётся копия заголовка, тип данных изменяется на binary;
- тело нового письма – простой html с кодировкой UTF-8;
- секция text/plain оригинального письма конвертируется в UTF-8 и вставляется в новое письмо;
- если в оригинальном письме есть таблицы, будет произведена попытка вставить их без изменения внутреннего форматирования в новое письмо. Эта часть работает плохо;
- после основного текста письма размещаются ссылки на распакованные вложения:
    Вложения: a href="file:///path" a href="ftp://path"
    см. скриншоты.

Выявленные проблемы на данный момент:
- html-вложения и xml-вложения не поддерживаются (код)
- apple mail и RFC5987 не поддерживаются (код)
- вложения, закодированные quoted-printable, не обрабатываются (код)
- знак = в именах вложений не поддерживается (код)
- битые имена файлов вложений не обрабатываются (напр. некорректные символы UTF) (код)
- письмо перекодируется: в качестве основы берётся plain text часть, далее анализируется html-часть. Если в html есть таблицы, попытка вставить их на место текста в текстовой части. В этой части часто происходят ошибки, и в этом случае таблицы вставляются в конец письма. Сложно форматированные письма бьются, но письмо в целом остаётся читаемым.
- inline pdf и другие нестандартные inline-элементы (не изображения) не обрабатываются
- замена конечных файлов по имени и размеру
- MAPI кодированные вложения не поддерживается
- имена файлов вложений без расширений (без .ext) не поддерживаются 

Программа далека от идеала, 
однако 
протестирована на 10000+ файлах,
и вполне позволяет оптимизировать систему хранения почты.

