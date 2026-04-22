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
            .desc("(Required) Lucene library to extract. Maven coordinate groupId:artifactId:version")
            .build();

    private static final Option force = Option.builder("f")
            .longOpt("force")
            .required(false)
            .desc("Force download even if the file already exists")
            .build();

    private static final Option downloadDir = Option.builder("dd")
            .longOpt("download-dir")
            .required(false)
            .hasArg()
            .argName("path")
            .desc("Directory to download the jar files to (default: ./download)")
            .build();

    private static final Option dependency = Option.builder("dep")
            .longOpt("dependency")
            .required(false)
            .hasArg()
            .argName("dependency")
            .desc("Additional Maven dependency to include on the classpath. Maven coordinate groupId:artifactId:version")
            .numberOfArgs(Option.UNLIMITED_VALUES)
            .build();

    private static final Option strict = Option.builder()
            .longOpt("strict")
            .required(false)
            .desc("Fail fast on classes that cannot be loaded (missing transitive types). Default: log and continue.")
            .build();

    private static final Option noVerifyChecksum = Option.builder()
            .longOpt("no-verify-checksum")
            .required(false)
            .desc("Skip SHA-1 checksum verification against Maven Central's published .sha1 sidecar.")
            .build();

    public static void main(String[] args) {
        if (args.length == 0) {
            printUsage(System.out);
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
                printUsage(System.err);
                System.exit(1);
                break;
        }
    }

    private static void printUsage(java.io.PrintStream out) {
        out.println("Usage: java -jar lucene-api-extractor.jar [command] [options]");
        out.println("Commands:");
        out.println("  extract: Extracts the API from the Lucene libraries");
        out.println("  hash: Extracts the API and then hashes the output");
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

        var cmd = parseOrExit(args, options, "extract");

        var context = buildContext(cmd, cmd.getOptionValue(output));

        try {
            ExtractRunner.extract(context);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    private static void hash(String[] args) {
        var options = getCommonOptions();
        var cmd = parseOrExit(args, options, "hash");

        var context = buildContext(cmd, null);

        try {
            ExtractRunner.printHash(context);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    private static CommandLine parseOrExit(String[] args, Options options, String command) {
        try {
            return new DefaultParser().parse(options, args);
        } catch (ParseException e) {
            System.err.println(e.getMessage());
            new HelpFormatter().printHelp(
                    "java -jar lucene-api-extractor.jar " + command + " [options]", options);
            System.exit(1);
            throw new IllegalStateException("unreachable");
        }
    }

    private static ExtractContext buildContext(CommandLine cmd, String outputValue) {
        return new ExtractContext(
                cmd.getOptionValue(downloadDir, "download"),
                cmd.getOptionValues(library),
                cmd.hasOption(force),
                outputValue,
                cmd.getOptionValues(dependency) == null ? new String[0] : cmd.getOptionValues(dependency),
                cmd.hasOption(strict),
                !cmd.hasOption(noVerifyChecksum));
    }

    private static Options getCommonOptions() {
        var options = new Options();

        options.addOption(library);
        options.addOption(force);
        options.addOption(downloadDir);
        options.addOption(dependency);
        options.addOption(strict);
        options.addOption(noVerifyChecksum);

        return options;
    }
}
