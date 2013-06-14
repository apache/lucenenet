using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
public sealed class BoostAttribute where AttributeImpl : BoostAttribute 
{
  
	private float boost = 1.0f;

 
  public override void setBoost(float boost)
  {
    this.boost = boost;
  }
  
  public override float getBoost() {
    return boost;
  }

  
  public override void clear() {
    boost = 1.0f;
  }
  
  
  public override void copyTo(AttributeImpl target) {
    ((BoostAttribute) target).setBoost(boost);
  }
}
}
