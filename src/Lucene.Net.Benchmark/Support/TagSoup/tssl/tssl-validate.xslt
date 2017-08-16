<!-- Generate complaints if the schema is invalid in some way.  -->

<!--
// This file is part of TagSoup and is Copyright 2002-2008 by John Cowan.
// 
// TagSoup is licensed under the Apache License,
// Version 2.0.  You may obtain a copy of this license at
// http://www.apache.org/licenses/LICENSE-2.0 .  You may also have
// additional legal rights not granted by this license.
//
// TagSoup is distributed in the hope that it will be useful, but
// unless required by applicable law or agreed to in writing, TagSoup
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, either express or implied; not even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
-->

<xsl:transform xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:tssl="http://www.ccil.org/~cowan/XML/tagsoup/tssl"
	version="1.0">

  <xsl:output method="text"/>

  <xsl:strip-space elements="*"/>

  <!-- Generates a report if an element does not belong to at least
       one of the groups that its parent element contains.  -->
  <xsl:template match="tssl:element/tssl:element">
    <xsl:if test="not(tssl:memberOfAny) and not(tssl:memberOf/@group = ../tssl:contains/@group)">
      <xsl:value-of select="@name"/>
      <xsl:text> is not in the content model of </xsl:text>
      <xsl:value-of select="../@name"/>
      <xsl:text>&#xA;</xsl:text>
    </xsl:if>
    <xsl:apply-templates/>
  </xsl:template>



</xsl:transform>
