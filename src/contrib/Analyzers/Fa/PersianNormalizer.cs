using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analyzers.Fa
{
/**
 * Normalizer for Persian.
 * <p>
 * Normalization is done in-place for efficiency, operating on a termbuffer.
 * <p>
 * Normalization is defined as:
 * <ul>
 * <li>Normalization of various heh + hamza forms and heh goal to heh.
 * <li>Normalization of farsi yeh and yeh barree to arabic yeh
 * <li>Normalization of persian keheh to arabic kaf
 * </ul>
 * 
 */
public class PersianNormalizer {
  public const char YEH = '\u064A';

  public const char FARSI_YEH = '\u06CC';

  public const char YEH_BARREE = '\u06D2';

  public const char KEHEH = '\u06A9';

  public const char KAF = '\u0643';

  public const char HAMZA_ABOVE = '\u0654';

  public const char HEH_YEH = '\u06C0';

  public const char HEH_GOAL = '\u06C1';

  public const char HEH = '\u0647';

  /**
   * Normalize an input buffer of Persian text
   * 
   * @param s input buffer
   * @param len length of input buffer
   * @return length of input buffer after normalization
   */
  public int Normalize(char[] s, int len) {

    for (int i = 0; i < len; i++) {
      switch (s[i]) {
      case FARSI_YEH:
      case YEH_BARREE:
        s[i] = YEH;
        break;
      case KEHEH:
        s[i] = KAF;
        break;
      case HEH_YEH:
      case HEH_GOAL:
        s[i] = HEH;
        break;
      case HAMZA_ABOVE: // necessary for HEH + HAMZA
        len = Delete(s, i, len);
        i--;
        break;
      default:
        break;
      }
    }

    return len;
  }

  /**
   * Delete a character in-place
   * 
   * @param s Input Buffer
   * @param pos Position of character to delete
   * @param len length of input buffer
   * @return length of input buffer after deletion
   */
  protected int Delete(char[] s, int pos, int len) {
    if (pos < len)
      Array.Copy(s, pos + 1, s, pos, len - pos - 1);
    
    return len - 1;
  }

}
}
