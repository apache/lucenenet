# Porting Guidance

(work in progress)

There are differences between C# and Java at the language level and there are differences at the framework level.


## Language Level Differences


### Flexible interfaces and enums.

Java allows classes in interfaces and methods inside of enums.  In C#, both these cases are illegal and will lead to compile errors.

```java

/**
 * interface with nested class
 */
public interface Bits
{
    public boolean get(int index);

    public int length();


    public class MatchAllBits: Bits
    {   
        final int len;

        public MatchAllBits(int len) {
            this.len = len;
        }

        @Override
        public boolean get(int index) {
          return true;
        }

        @Override
        public int length() {
          return len;
        }
    }
}


/**
 *
 */
public enum Version {
    
    @Deprecated
    LUCENE_4_10,

    LUCENE_5_0,

    @Deprecated
    LUCENE_CURRENT

    public boolean onOrAfter(Version other) {
        return compareTo(other) >= 0;
    }
  
    public static Version parseLeniently(String version) {
        final String parsedMatchVersion = version
            .toUpperCase(Locale.ROOT)
            .replaceFirst("^(\\d+)\\.(\\d+)$", "LUCENE_$1_$2")
            .replaceFirst("^LUCENE_(\\d)(\\d)$", "LUCENE_$1_$2");
        return Version.valueOf(parsedMatchVersion);
    }
} 

```

When porting the above code in C#, the methods and classes need to extracted. For example, extract the interface into IBits. Extract the methods from the enum into its own static Util class.  

```c#
public interface IBits
{
    bool this[int index];

    int Length { get; set; }
}

public class Bits
{
    public class MatchAllBits: Bits
    {   
        final int len;

        public MatchAllBits(int len) {
            this.len = len;
        }

        @Override
        public boolean get(int index) {
          return true;
        }

        @Override
        public int length() {
          return len;
        }
    }
}


public enum Version {
    
    @Deprecated
    LUCENE_4_10,

    LUCENE_5_0,

    @Deprecated
    LUCENE_CURRENT;
} 

public static class VersionUtil
{
    public static boolean OnOrAfter(Version current, Version other) 
    {
        return current.CompareTo(other) >= 0;
    }
  
    public static Version ParseLeniently(String version) 
    {
        throw new NotImplementedException();
    }
}

```