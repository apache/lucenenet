Apache Rat Ant Task Library
===========================

The Ant Task Library provides a single Ant task and a few supporting Ant types to run Rat,
the Release Audit Tool from inside Apache Ant.

Using Ant's resource abstraction the task can be used to check files on disk as well as tarballs
or even URLs directly.

Requirements
------------

The Rat Ant Task Library requires Apache Ant 1.7.1 or higher (it works well with 1.8.x)
It also requires at least Java 1.5.

Installation
------------

There are several ways to use the Antlib:

    The traditional way:

    <taskdef
        resource="org/apache/rat/anttasks/antlib.xml">
        <classpath>
            <pathelement location="YOUR-PATH-TO/apache-rat-${project.version}.jar"/>
        </classpath>
    </taskdef>

    With this you can use the report task like plain Ant tasks, they'll live in the default namespace.
    I.e. if you can run exec without any namespace prefix, you can do so for report as well.
    Similar, but assigning a namespace URI

    <taskdef
        uri="antlib:org.apache.rat.anttasks"
        resource="org/apache/rat/anttasks/antlib.xml">
        <classpath>
            <pathelement location="YOUR-PATH-TO/apache-rat-${project.version}.jar"/>
        </classpath>
    </taskdef>

    This puts your task into a separate namespace than Ant's namespace. You would use the tasks like

    <project
        xmlns:rat="antlib:org.apache.rat.anttasks"
        xmlns="antlib:org.apache.tools.ant">
        ...
        <rat:report>
            <fileset dir="src"/>
        </rat:report>

    or a variation thereof.
    Using Ant's autodiscovery. Place apache-rat-tasks.jar and all dependencies into a directory
    and use ant -lib YOUR-PATH-TO/apache-rat-${project.version}.jar
    or copy apache-rat-${project.version}.jar into ANT_HOME/lib.
    
    Then in your build file, simply declare the namespace on the project tag:

    <project
        xmlns:rat="antlib:org.apache.rat.anttasks"
        xmlns="antlib:org.apache.tools.ant">

    All tasks of this library will automatically be available in the rat namespace without any taskdef.
