# Lucene.Net.Util.Packed

## Generating the files

There are four Python scripts in this folder:
[`gen_BulkOperation.py`](gen_BulkOperation.py),
[`gen_Direct.py`](gen_Direct.py),
[`gen_Packed64SingleBlock.py`](gen_Packed64SingleBlock.py), and
[`gen_PackedThreeBlocks.py`](gen_PackedThreeBlocks.py).
These scripts were ported from the original Lucene 4.8.1 code, and modified to generate the corresponding C# code instead of Java.

To generate the files, run the following commands from this directory:

```sh
python3 ./gen_BulkOperation.py
python3 ./gen_Direct.py
python3 ./gen_Packed64SingleBlock.py
python3 ./gen_PackedThreeBlocks.py
```

These scripts do not require any virtual environment or additional packages to run.
All have been tested with Python 3 and updated as necessary from the original Python 2 code.
As Python 2 is now out of support, they have not been tested against Python 2.

As noted in the comment on the generated files, please do not modify the generated files directly, now that we have these scripts.
Instead, modify the scripts as needed and re-generate the files.

A good test to ensure the generated files are in sync with the scripts is to run the scripts, and then see if there are any pending Git changes in the repository.
