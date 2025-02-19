/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package org.apache.lucenenet.lucene.api.extractor;

import org.apache.commons.cli.*;

public class Main {
    private static final Option library = Option.builder("lib")
            .longOpt("library")
            .required()
            .hasArg()
            .argName("library")
            .numberOfArgs(Option.UNLIMITED_VALUES)
            .desc("(Required) Lucene library to extract. Should be in the Maven Coordinates format groupId:artifactId:version")
            .build();

    private static final Option force = Option.builder("f")
            .longOpt("force")
            .required(false)
            .desc("Force download even if the file already exists")
            .build();

    private static final Option downloadDir = Option.builder("dd")
            .longOpt("download-dir")
            .required(false)
            .desc("Directory to download the jar files to (default: ./downloads)")
            .build();

    private static final Option dependency = Option.builder("dep")
            .longOpt("dependency")
            .required(false)
            .desc("Additional Maven dependencies to include in the classpath. Should be in the Maven Coordinates format groupId:artifactId:version")
            .numberOfArgs(Option.UNLIMITED_VALUES)
            .build();

    public static void main(String[] args) {
        if (args.length == 0) {
            printUsage();
            return;
        }

        var command = args[0];

        switch (command) {
            case "extract":
                extract(args);
                break;
            case "hash":
                hash(args);
                break;
            default:
                System.err.println("Unknown command: " + command);
                printUsage();
                break;
        }
    }

    private static void printUsage() {
        System.out.println("Usage: java -jar lucene-api-extractor.jar [command] [options]");
        System.out.println("Commands:");
        System.out.println("  extract: Extracts the API from the Lucene libraries");
        System.out.println("  hash: Extracts the API and then hashes the output");
        System.exit(1);
    }

    private static void extract(String[] args) {
        var options = getCommonOptions();

        var output = Option.builder("o")
                .longOpt("output")
                .required(false)
                .hasArg()
                .argName("output")
                .desc("Output file for the extracted API JSON (defaults to standard output)")
                .build();
        options.addOption(output);

        var parser = new DefaultParser();

        var formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(options, args);
        } catch (ParseException e) {
            System.err.println(e.getMessage());
            formatter.printHelp("java -jar lucene-api-extractor.jar extract [options]", options);

            System.exit(1);
            return;
        }

        var librariesValue = cmd.getOptionValues(library);
        var forceValue = cmd.hasOption(force);
        var downloadDirValue = cmd.getOptionValue(downloadDir, "download");
        var outputValue = cmd.getOptionValue(output);
        var dependencyValues = cmd.getOptionValues(dependency);

        var context = new ExtractContext(downloadDirValue,
                librariesValue,
                forceValue,
                outputValue,
                dependencyValues);

        try {
            ExtractRunner.extract(context);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    private static void hash(String[] args) {
        var options = getCommonOptions();
        var parser = new DefaultParser();

        var formatter = new HelpFormatter();
        CommandLine cmd;

        try {
            cmd = parser.parse(options, args);
        } catch (ParseException e) {
            System.err.println(e.getMessage());
            formatter.printHelp("java -jar lucene-api-extractor.jar hash [options]", options);

            System.exit(1);
            return;
        }

        var librariesValue = cmd.getOptionValues(library);
        var forceValue = cmd.hasOption(force);
        var downloadDirValue = cmd.getOptionValue(downloadDir, "download");
        var dependencyValues = cmd.getOptionValues(dependency);

        var context = new ExtractContext(downloadDirValue,
                librariesValue,
                forceValue,
                null,
                dependencyValues);

        try {
            ExtractRunner.printHash(context);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    private static Options getCommonOptions() {
        var options = new Options();

        options.addOption(library);
        options.addOption(force);
        options.addOption(downloadDir);
        options.addOption(dependency);

        return options;
    }
}
