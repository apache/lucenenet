<!-- Generate Java code to be inserted into HTMLModels.java.  -->

<!--
// This file is part of TagSoup and is Copyright 2002-2008 by John Cowan.
// 
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

  <!-- The main template.  We are going to generate Java constant
       definitions for the groups in the file.  -->
  <xsl:template match="tssl:schema">
    <xsl:apply-templates select="tssl:group">
      <xsl:sort select="@id"/>
    </xsl:apply-templates>
  </xsl:template>

  <!-- Generate a declaration for a single group.  -->
  <xsl:template match="tssl:group" name="tssl:group">
    <xsl:param name="id" select="@id"/>
    <xsl:param name="number" select="position()"/>
    <xsl:text>&#x9;public const int </xsl:text>
    <xsl:value-of select="$id"/>
    <xsl:text> = 1 &lt;&lt; </xsl:text>
    <xsl:value-of select="$number"/>
    <xsl:text>;&#xA;</xsl:text>
  </xsl:template>

</xsl:transform>
