<!-- Generate Java code to be inserted into HTMLSchema.java.  -->

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

  <!-- The main template.  This generates calls on the Schema routines
       setURI(), setPrefix(), elementType(), parent(), attribute(),
       and entity() in that order.  Several special cases are
       handled by template calls.  -->
  <xsl:template match="tssl:schema">
    <!-- setURI() -->
    <xsl:text>&#x9;&#x9;Uri = "</xsl:text>
    <xsl:value-of select="@ns"/>
    <xsl:text>";&#xA;</xsl:text>
    <!-- setPrefix() -->
    <xsl:text>&#x9;&#x9;Prefix = "</xsl:text>
    <xsl:value-of select="@prefix"/>
    <xsl:text>";&#xA;</xsl:text>
    <!-- elementType() special cases -->
    <xsl:text>&#x9;&#x9;ElementType("&lt;pcdata>", M_EMPTY, M_PCDATA, 0);&#xA;</xsl:text>
    <xsl:text>&#x9;&#x9;ElementType("&lt;root>", </xsl:text>
    <xsl:apply-templates select="tssl:element/tssl:isRoot"/>
    <xsl:text>, M_EMPTY, 0);&#xA;</xsl:text>
    <!-- elementType() main loop -->
    <xsl:apply-templates select="//tssl:element">
      <xsl:sort select="@name"/>
    </xsl:apply-templates>
    <!-- parent() special cases -->
    <xsl:call-template name="parent">
      <xsl:with-param name="elem" select="'&lt;pcdata>'"/>
      <xsl:with-param name="parent" select="//tssl:element[@text-parent='true']/@name"/>
    </xsl:call-template>
    <xsl:call-template name="parent">
      <xsl:with-param name="elem" select="tssl:element/@name"/>
      <xsl:with-param name="parent" select="'&lt;root>'"/>
    </xsl:call-template>
    <!-- parent() main loop -->
    <xsl:apply-templates select="//tssl:element/tssl:element" mode="parent">
      <xsl:sort select="@name"/>
    </xsl:apply-templates>
    <xsl:apply-templates select="//tssl:element/tssl:attribute">
      <xsl:sort select="../@name"/>
      <xsl:sort select="@name"/>
    </xsl:apply-templates>
    <!-- attribute() main loop -->
    <xsl:apply-templates select="tssl:attribute">
      <xsl:sort select="@name"/>
    </xsl:apply-templates>
    <!-- entity() main loop -->
    <xsl:apply-templates select="tssl:entity">
      <xsl:sort select="@name"/>
    </xsl:apply-templates>
  </xsl:template>

  <!-- Generates a single call to elementType().  -->
  <xsl:template match="tssl:element">
    <xsl:text>&#x9;&#x9;ElementType("</xsl:text>
    <xsl:value-of select="@name"/>
    <xsl:text>", </xsl:text>
    <xsl:choose>
      <xsl:when test="@type = 'element'">
        <xsl:apply-templates select="tssl:contains"/>
      </xsl:when>
      <xsl:when test="@type = 'string'">
        <xsl:text>M_PCDATA</xsl:text>
      </xsl:when>
      <xsl:when test="@type = 'mixed'">
        <xsl:text>M_PCDATA|</xsl:text>
        <xsl:apply-templates select="tssl:contains"/>
      </xsl:when>
      <xsl:when test="@type = 'empty'">
        <xsl:text>M_EMPTY</xsl:text>
      </xsl:when>
      <xsl:when test="@type = 'any'">
        <xsl:text>M_ANY</xsl:text>
      </xsl:when>
      <xsl:when test="@type = 'cdata'">
        <xsl:text>M_PCDATA</xsl:text>
      </xsl:when>
    </xsl:choose>
    <xsl:text>, </xsl:text>
    <xsl:apply-templates select="tssl:memberOf"/>
    <xsl:apply-templates select="tssl:memberOfAny"/>
    <xsl:apply-templates select="tssl:isRoot"/>
    <xsl:text>, </xsl:text>
    <xsl:choose>
      <xsl:when test="@type = 'cdata'">
        <xsl:text>F_CDATA</xsl:text>
      </xsl:when>
      <xsl:when test="@closeMode = 'restartable'">
        <xsl:text>F_RESTART</xsl:text>
      </xsl:when>
      <xsl:when test="@closeMode = 'unclosable'">
        <xsl:text>F_NOFORCE</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>0</xsl:text>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:text>);&#xA;</xsl:text>
  </xsl:template>

  <!-- Applied from tssl:element to generate the contains argument.  -->
  <xsl:template match="tssl:contains">
    <xsl:value-of select="@group"/>
    <xsl:if test="position() != last()">
      <xsl:text>|</xsl:text>
    </xsl:if>
  </xsl:template>

  <!-- Applied from tssl:element to generate the memberOf argument.  -->
  <xsl:template match="tssl:memberOf">
    <xsl:value-of select="@group"/>
    <xsl:if test="position() != last()">
      <xsl:text>|</xsl:text>
    </xsl:if>
  </xsl:template>

  <!-- Applied from tssl:element to handle memberOfAny.  -->
  <xsl:template match="tssl:memberOfAny">
    <xsl:text>(int)(M_ANY &amp; ~M_ROOT)</xsl:text>
  </xsl:template>

  <!-- Applied from tssl:element to handle isRoot.  -->
  <xsl:template match="tssl:isRoot">
    <xsl:text>M_ROOT</xsl:text>
  </xsl:template>

  <!-- Generates a single call to parent().  The mode is used to prevent XSLT
       from getting confused and generating elementType calls instead.  -->
  <xsl:template match="tssl:element/tssl:element" name="parent" mode="parent">
    <xsl:param name="elem" select="@name"/>
    <xsl:param name="parent" select="../@name"/>
    <xsl:text>&#x9;&#x9;Parent("</xsl:text>
    <xsl:value-of select="$elem"/>
    <xsl:text>", "</xsl:text>
    <xsl:value-of select="$parent"/>
    <xsl:text>");&#xA;</xsl:text>
  </xsl:template>

  <!-- Generates a single call to attribute().  -->
  <xsl:template match="tssl:element/tssl:attribute" name="tssl:attribute">
    <xsl:param name="elem" select="../@name"/>
    <xsl:param name="attr" select="@name"/>
    <xsl:param name="type" select="@type"/>
    <xsl:param name="default" select="@default"/>
    <xsl:text>&#x9;&#x9;Attribute("</xsl:text>
    <xsl:value-of select="$elem"/>
    <xsl:text>", "</xsl:text>
    <xsl:value-of select="$attr"/>
    <xsl:text>", "</xsl:text>
    <xsl:choose>
      <xsl:when test="$type">
        <xsl:value-of select="$type"/>
      </xsl:when>
      <xsl:when test="not($type)">
        <xsl:text>CDATA</xsl:text>
      </xsl:when>
    </xsl:choose>
    <xsl:text>", </xsl:text>
    <xsl:choose>
      <xsl:when test="$default">
        <xsl:text>"</xsl:text>
        <xsl:value-of select="$default"/>
        <xsl:text>"</xsl:text>
      </xsl:when>
      <xsl:when test="not($default)">
        <xsl:text>null</xsl:text>
      </xsl:when>
    </xsl:choose>
    <xsl:text>);&#xA;</xsl:text>
  </xsl:template>

  <!-- Generates calls to attribute() (using the above template)
       based on the global attribute definitions.  -->
  <xsl:template match="tssl:schema/tssl:attribute">
    <xsl:variable name="attr" select="@name"/>
    <xsl:variable name="type" select="@type"/>
    <xsl:variable name="default" select="@default"/>
    <xsl:for-each select="//tssl:element">
      <xsl:sort select="@name"/>
      <xsl:call-template name="tssl:attribute">
        <xsl:with-param name="elem" select="@name"/>
        <xsl:with-param name="attr" select="$attr"/>
        <xsl:with-param name="type" select="$type"/>
        <xsl:with-param name="default" select="$default"/>
      </xsl:call-template>
    </xsl:for-each>
  </xsl:template>

  <!-- Generates a single call to entity().  -->
  <xsl:template match="tssl:entity">
    <xsl:text>&#x9;&#x9;Entity("</xsl:text>
    <xsl:value-of select="@name"/>
    <xsl:text>", 0x</xsl:text>
    <xsl:value-of select="@codepoint"/>
    <xsl:text>);&#xA;</xsl:text>
  </xsl:template>

</xsl:transform>
