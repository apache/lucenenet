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
	xmlns:stml="http://www.ccil.org/~cowan/XML/tagsoup/stml"
	version="1.0">

  <xsl:output method="text"/>

  <xsl:strip-space elements="*"/>

  <!-- The main template.  Generates declarations for states and
       actions, then the statetable itself, and then a comment (used for
       manual checking) listing all the actions compactly.  -->
  <xsl:template match="stml:statetable">
    <xsl:apply-templates select="stml:state">
      <xsl:sort select="@id"/>
    </xsl:apply-templates>

    <xsl:apply-templates select="stml:action">
      <xsl:sort select="@id"/>
    </xsl:apply-templates>

    <xsl:text>&#x9;private static int[] statetable = {&#xA;</xsl:text>
    <xsl:apply-templates select="stml:state/stml:tr">
      <xsl:sort select="../@id"/>
      <xsl:sort select="@symbol"/>
      <xsl:sort select="@char"/>
    </xsl:apply-templates>
    <xsl:text>&#xA;&#x9;};&#xA;</xsl:text>

    <xsl:text>&#x9;private static readonly string[] debug_actionnames = { ""</xsl:text>
    <xsl:apply-templates select="stml:action" mode="debug">
      <xsl:sort select="@id"/>
    </xsl:apply-templates>
    <xsl:text>};&#xA;</xsl:text>

    <xsl:text>&#x9;private static readonly string[] debug_statenames = { ""</xsl:text>
    <xsl:apply-templates select="stml:state" mode="debug">
      <xsl:sort select="@id"/>
    </xsl:apply-templates>
    <xsl:text>};&#xA;</xsl:text>

    <xsl:text>&#xA;</xsl:text>
  </xsl:template>

  <!-- Generate a single state declaration.  -->
  <xsl:template match="stml:state">
    <xsl:text>&#x9;private const int </xsl:text>
    <xsl:value-of select="@id"/>
    <xsl:text> = </xsl:text>
    <xsl:value-of select="position()"/>
    <xsl:text>;&#xA;</xsl:text>
  </xsl:template>

  <!-- Generate a single action declaration.  -->
  <xsl:template match="stml:action">
    <xsl:text>&#x9;private const int </xsl:text>
    <xsl:value-of select="@id"/>
    <xsl:text> = </xsl:text>
    <xsl:value-of select="position()"/>
    <xsl:text>;&#xA;</xsl:text>
  </xsl:template>

  <!-- Generate a single row of the statetable.  -->
  <xsl:template match="stml:tr">
    <xsl:choose>
      <xsl:when test="@symbol = 'EOF'">
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char" select="&quot;-1&quot;"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="@symbol = 'LF'">
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char" select="&quot;'\n'&quot;"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="@symbol = 'default'">
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char" select="&quot;0&quot;"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="@char = &quot;&apos;&quot;">
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char" select="&quot;'\''&quot;"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="@symbol = 'S'">
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char" select="&quot;' '&quot;"/>
        </xsl:call-template>
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char" select="&quot;'\n'&quot;"/>
        </xsl:call-template>
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char" select="&quot;'\t'&quot;"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:call-template name="dump-tr">
          <xsl:with-param name="char"
		select="concat(&quot;'&quot;, @char, &quot;'&quot;)"/>
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- This is a subroutine used to do the actual printing. -->
  <xsl:template name="dump-tr">
    <xsl:param name="char"/>
    <xsl:text>&#x9;&#x9;</xsl:text>
    <xsl:value-of select="../@id"/>
    <xsl:text>, </xsl:text>
    <xsl:value-of select="$char"/>
    <xsl:text>, </xsl:text>
    <xsl:value-of select="@action"/>
    <xsl:text>, </xsl:text>
    <xsl:value-of select="@newstate"/>
    <xsl:text>,&#xA;</xsl:text>
  </xsl:template>

  <!-- Generate a single action name in the "Actions:" comment.
        The mode is used to keep XSLT from confusing this with the
        regular actions template that does the action declarations.  -->
  <xsl:template match="stml:action" mode="debug">
    <xsl:text>, "</xsl:text>
    <xsl:value-of select="@id"/>
    <xsl:text>"</xsl:text>
  </xsl:template>

  <!-- Generate a single stat debug name.  -->
  <xsl:template match="stml:state" mode="debug">
    <xsl:text>, "</xsl:text>
    <xsl:value-of select="@id"/>
    <xsl:text>"</xsl:text>
  </xsl:template>

</xsl:transform>
