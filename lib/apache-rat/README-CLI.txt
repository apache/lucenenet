About Apache Rat™
================

Rat audits software distributions, with a special interest in headers. 
If this isn't quite what you're looking for then take a look at the 
other products developed by Apache Creadur™, 
including Apache Whisker™ which audits and generates legal (for example LICENSE)
documents for complex software distributions.

Running from the Command Line
-----------------------------

Run from the command line with:

java -jar apache-rat-${project.version}.jar --help

This will output a help message detailing the command line options available to you.

Adding license headers
----------------------

Rat can be used to automatically add license headers to files that do not currently have them. 
Only files that are not excluded by the Rat configurations will be affected.

To add license headers use a command such as:

java -jar apache-rat-${project.version}.jar --addlicense
  --copyright "Copyright 2008 Foo" --force
  /path/to/project

This command will add the license header directly to the source files. 
If you prefer to see which files will be changed and how then remove the "--force" option.
Using multiple excludes from a file

It is common to use the Rat with the maven or ant plugins and specify a series of files to exclude
(such as a README or version control files). 
If you are using the Rat application instead of a plugin you can specify a series of regex excludes
in a file and specify that with the -E option.

java -jar apache-rat-${project.version}.jar
 -E /path/to/project/.rat-excludes
 -d /path/to/project

Command Line Options
====================

usage: java rat.report [options] [DIR|TARBALL]
Options
 -A,--addLicense                Add the default license header to any file
                                with an unknown license that is not in the
                                exclusion list. By default new files will
                                be created with the license header, to
                                force the modification of existing files
                                use the --force option.
 -a,--addlicense                Add the default license header to any file
                                with an unknown license that is not in the
                                exclusion list. By default new files will
                                be created with the license header, to
                                force the modification of existing files
                                use the --force option.
 -c,--copyright <arg>           The copyright message to use in the
                                license headers, usually in the form of
                                "Copyright 2008 Foo"
 -d,--dir                       Used to indicate source when using
                                --exclude
 -E,--exclude-file <fileName>   Excludes files matching regular expression
                                in <file> Note that --dir is required when
                                using this parameter.
 -e,--exclude <expression>      Excludes files matching wildcard
                                <expression>. Note that --dir is required
                                when using this parameter. Allows multiple
                                arguments.
 -f,--force                     Forces any changes in files to be written
                                directly to the source files (i.e. new
                                files are not created)
 -h,--help                      Print help for the Rat command line
                                interface and exit
 -s,--stylesheet <arg>          XSLT stylesheet to use when creating the
                                report.  Not compatible with -x
 -x,--xml                       Output the report in raw XML format.  Not
                                compatible with -s
