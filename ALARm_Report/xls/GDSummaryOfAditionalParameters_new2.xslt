<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" version="1.0" exclude-result-prefixes="msxsl">
	<xsl:output method="html"/>
	<xsl:template match="/">
		<html>
			<head>
				<title>ГД дополнительных параметров с результатами оценки отступлений</title>
				<style>

					.addparam {
					page-break-before: always;
					margin-top: auto;
					margin-left: auto;
					margin-right: auto;
					width: 820;

					}

					.pickets {
					transform: rotate(90deg);
					position: relative;
					top: -433;
					height: 11px;
					font-size: 10;
					width: 865;
					left: 286px;
					}
					.picket {
					float: left;
					margin-right: 89;
					}
					.bottom-labels {
					transform: rotate(90deg);
					top: -268;
					font-size: 9px;
					left: 422px;
					width: 20px;
					height: 550px;
					position: relative;
					margin-bottom: -550px;
					text-align: right;
					}
					.top-title {
					margin-top: 15px;
					margin-right: 15px;
					width: 600px;
					height: 24px;
					border-width: thin;
					padding-right: 3px;
					text-align: right;
					font-size: 11;
					font-family: Arial;
					margin-left: auto;
					margin-bottom: 20px;
					border-right: solid;
					border-bottom: solid;
					border-left: solid;
					border-width: thin;
					border-color: dimgray;
					}
					.main-chart {

					margin-left: auto;
					position: relative;

					}
					.chart {
					margin-right: 0px;
					margin-left: auto;
					margin-bottom: 0.5px solid;
					}
					.right-title {
					position: relative;
					left: 250px;
					top: 505px;
					transform: rotate(90deg);
					font-size: 9.5;
					font-family: Arial;
					width:1000px;
					}
					.inform-title {
					display: block;
					font-family: monospace;
					font-size: 9.5;
					white-space: pre;
					margin: 1em 0px;
					color: darkgray;
					border-color: darkgray;


					}
					.button {
					background-color: #4CAF50; /* Green */
					border: none;
					color: white;
					padding: 8px 16px;
					text-align: center;
					text-decoration: none;
					display: inline-block;
					font-size: 12px;
					margin: 2px 3px;
					-webkit-transition-duration: 0.4s; /* Safari */
					transition-duration: 0.4s;
					cursor: pointer;
					background-color: white;
					color: darkgray;
					border: 2px solid #4CAF50;
					position: fixed;
					right: 10px;
					top:10px;
					}
					.button:hover {
					background-color: #4CAF50;
					color: white;
					}
					.nkm2 {
					font-size: 14;
					font-family: Arial;
					text-align: right;
					margin-right: 5px;
					}
					.nkm1 {
					font-size: 14;
					font-family: Arial;
					float: left;
					margin-left: -80;
					padding-right: 55;
					}


				</style>
				<style type="text/css" media="print">
					.dontprint
					{
					display: none;
					}

				</style>

			</head>
			<body>
				<!-- <button class="dontprint button" onclick="hidetitle();">Режим ленты</button> -->

				<xsl:for-each select="report/addparam">
					<div  class = "addparam">

						<div class = "main-chart">
							<svg class= "chart">

								<xsl:attribute name="viewBox">
									<xsl:value-of select="@viewbox" />
								</xsl:attribute>

								<text x="765" style="font-size: 12;" text-anchor="end">
									<xsl:attribute name="y">
										<xsl:value-of select="@topx1" />
									</xsl:attribute>
									<xsl:value-of select="@top-title"/>
								</text>
								<text x="765" style="font-size: 12;" text-anchor="end">
									<xsl:attribute name="y">
										<xsl:value-of select="@topx2" />
									</xsl:attribute>
									<xsl:value-of select="@speedlimit"/>
								</text>
								<rect fill="none" stroke="black" x="217" width="550" height="35">
									<xsl:attribute name="y">
										<xsl:value-of select="@topr" />
									</xsl:attribute>
								</rect>


								<text transform="rotate(90)" font-size="11" y="-770">
									<xsl:attribute name="x">
										<xsl:value-of select="@topx" />
									</xsl:attribute>
									<xsl:value-of select="@right-title"/>
								</text>

								<text fill="black" x="0" font-size="10">
									<xsl:attribute name="y">
										<xsl:value-of select="@topf" />
									</xsl:attribute>
									<xsl:value-of select="@fragment" />
								</text>

								<!-- 
                                <text x="1" style="transform: rotate(90deg);font-size: 7px;text-align: right;white-space:pre;" y="-694.7678">1524</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7px;text-align: right;white-space:pre;" y="-684.74195">1520</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-668.6903">1512</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-716.8452">1536</text>

                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-628.5613">0</text>

                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-583.2155">1/12</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7px;text-align: right;white-space:pre;" y="-571.0513">1/16</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7px;text-align: right;white-space:pre;" y="-563.7529">1/20</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-544.2903">1/60</text>

                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-527.4361">1/12</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-515.272">1/16</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-507.9735">1/20</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-488.511">1/60</text>

                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-468.8477">1/12</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-456.6836">1/16</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-449.3852">1/20</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-429.9226">1/60</text>

                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-412.6671">1/12</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-400.503">1/16</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-393.2045">1/20</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-373.7419">1/60</text>


                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-354.8555">20</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-342.4958">13</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-332.6674">8</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-319.541931">0</text>

                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-299.6748">20</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-286.3151">13</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-276.4867">8</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-263.3613">0</text>


                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-242.3032">30</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-235.9495">25</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-219.245163">0</text>
                                
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-208.1226">30</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-201.7688">25</text>
                                <text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-175.103226">0</text> -->



								<!-- <xsl:for-each select="xgrid">
                                    <line stroke-dasharray="3,3" stroke-width="0.3" stroke="black" fill="none">
                                        <xsl:attribute name="x1">
                                            <xsl:value-of select="@x0" />
                                        </xsl:attribute>
                                        <xsl:attribute name="x2">
                                            <xsl:value-of select="@x0" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y1">
                                            <xsl:value-of select="../@minY" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y2">
                                            <xsl:value-of select="../@maxY" />
                                        </xsl:attribute>
                                    </line>
                                    <line stroke-dasharray="3,3" stroke-width="0.3" stroke="black" fill="none">
                                        <xsl:attribute name="x1">
                                            <xsl:value-of select="@x1" />
                                        </xsl:attribute>
                                        <xsl:attribute name="x2">
                                            <xsl:value-of select="@x1" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y1">
                                            <xsl:value-of select="../@minY" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y2">
                                            <xsl:value-of select="../@maxY" />
                                        </xsl:attribute>
                                    </line>
                                    <line stroke-dasharray="3,3" stroke-width="0.3" stroke="black" fill="none">
                                        <xsl:attribute name="x1">
                                            <xsl:value-of select="@x2" />
                                        </xsl:attribute>
                                        <xsl:attribute name="x2">
                                            <xsl:value-of select="@x2" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y1">
                                            <xsl:value-of select="../@minY" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y2">
                                            <xsl:value-of select="../@maxY" />
                                        </xsl:attribute>
                                    </line>
                                    <line stroke-dasharray="3,3" stroke-width="0.3" stroke="black" fill="none">
                                        <xsl:attribute name="x1">
                                            <xsl:value-of select="@x3" />
                                        </xsl:attribute>
                                        <xsl:attribute name="x2">
                                            <xsl:value-of select="@x3" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y1">
                                            <xsl:value-of select="../@minY" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y2">
                                            <xsl:value-of select="../@maxY" />
                                        </xsl:attribute>
                                    </line>
                                    <line stroke-dasharray="3,3" stroke-width="0.3" stroke="black" fill="none">
                                        <xsl:attribute name="x1">
                                            <xsl:value-of select="@x31" />
                                        </xsl:attribute>
                                        <xsl:attribute name="x2">
                                            <xsl:value-of select="@x31" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y1">
                                            <xsl:value-of select="../@minY" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y2">
                                            <xsl:value-of select="../@maxY" />
                                        </xsl:attribute>
                                    </line>
                                    <line stroke-dasharray="3,3" stroke-width="0.3" stroke="black" fill="none">
                                        <xsl:attribute name="x1">
                                            <xsl:value-of select="@x32" />
                                        </xsl:attribute>
                                        <xsl:attribute name="x2">
                                            <xsl:value-of select="@x32" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y1">
                                            <xsl:value-of select="../@minY" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y2">
                                            <xsl:value-of select="../@maxY" />
                                        </xsl:attribute>
                                    </line>
                                </xsl:for-each> -->

								<xsl:for-each select="xgrid/x">
									<line stroke-dasharray="0.5,5" stroke-width="0.5" stroke="gray" fill="none">
										<xsl:attribute name="x1">
											<xsl:value-of select="." />
										</xsl:attribute>
										<xsl:attribute name="x2">
											<xsl:value-of select="." />
										</xsl:attribute>
										<xsl:attribute name="y1">
											<xsl:value-of select="../../@minY" />
										</xsl:attribute>
										<xsl:attribute name="y2">
											<xsl:value-of select="../../@maxY" />
										</xsl:attribute>
									</line>
									<text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-{.-2}">
										<xsl:value-of select="@text" />
									</text>
								</xsl:for-each>

								<xsl:for-each select="xgrid/L">
									<line stroke-dasharray="3,3" stroke-width="0.3" stroke="black" fill="none">
										<xsl:attribute name="x1">
											<xsl:value-of select="." />
										</xsl:attribute>
										<xsl:attribute name="x2">
											<xsl:value-of select="." />
										</xsl:attribute>
										<xsl:attribute name="y1">
											<xsl:value-of select="../../@minY" />
										</xsl:attribute>
										<xsl:attribute name="y2">
											<xsl:value-of select="../../@maxY" />
										</xsl:attribute>
									</line>

									<text x="1" style="transform: rotate(90deg);font-size: 7;text-align: right; white-space:pre" y="-{.-2}">
										<xsl:value-of select="@text" />
									</text>

								</xsl:for-each>

								<!-- <xsl:for-each select="xgrid/x20">
                                    <line stroke-dasharray="3,3" stroke-width="0.5" stroke="gray" fill="none">
                                        <xsl:attribute name="x1">
                                            <xsl:value-of select="." />
                                        </xsl:attribute>
                                        <xsl:attribute name="x2">
                                            <xsl:value-of select="." />
                                        </xsl:attribute>
                                        <xsl:attribute name="y1">
                                            <xsl:value-of select="../../@minY" />
                                        </xsl:attribute>
                                        <xsl:attribute name="y2">
                                            <xsl:value-of select="../../@maxY" />
                                        </xsl:attribute>
                                    </line>
                                </xsl:for-each> -->
								<xsl:for-each select="gaps/g">
									<rect height="0.5">
										<xsl:attribute name="width">
											<xsl:value-of select="@w" />
										</xsl:attribute>
										<xsl:attribute name="x">
											<xsl:value-of select="@x" />
										</xsl:attribute>
										<xsl:attribute name="y">
											<xsl:value-of select="@y" />
										</xsl:attribute>
									</rect>
								</xsl:for-each>

								<xsl:for-each select="gaps/zero">
									<circle cx="{@x}" cy="{@y}" r="1" stroke="gray" stroke-width="1" fill="black"></circle>
								</xsl:for-each>


								<xsl:for-each select="polyline">
									<polyline style="fill:none;stroke:dimgray;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:0.3">
										<xsl:attribute name="points">
											<xsl:value-of select="@points" />
										</xsl:attribute>
									</polyline>
								</xsl:for-each>
								<xsl:for-each select="lines/line">
									<polyline style="fill:none;stroke:dimgray;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:0.3">
										<xsl:attribute name="points">
											<xsl:value-of select="." />
										</xsl:attribute>
									</polyline>
								</xsl:for-each>
								<xsl:for-each select="artcons/line">
									<polyline style="fill:none;stroke:dimgray;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:0.3">
										<xsl:attribute name="points">
											<xsl:value-of select="." />
										</xsl:attribute>
									</polyline>
								</xsl:for-each>

								<xsl:for-each select="impulsleft/imp">
									<line stroke-width="0.5" stroke="darkgray" fill="none" marker-end="url(#b-circle)">
										<xsl:attribute name="x1">
											<xsl:value-of select="../@x1" />
										</xsl:attribute>
										<xsl:attribute name="x2">
											<xsl:value-of select="@x2" />
										</xsl:attribute>
										<xsl:attribute name="y1">
											<xsl:value-of select="@y" />
										</xsl:attribute>
										<xsl:attribute name="y2">
											<xsl:value-of select="@y"/>
										</xsl:attribute>
									</line>
									<line stroke-width="0.5" stroke="darkgray" fill="none" marker-end="url(#b-circle)">
										<xsl:attribute name="x1">
											<xsl:value-of select="../@x3" />
										</xsl:attribute>
										<xsl:attribute name="x2">
											<xsl:value-of select="@x4" />
										</xsl:attribute>
										<xsl:attribute name="y1">
											<xsl:value-of select="@y" />
										</xsl:attribute>
										<xsl:attribute name="y2">
											<xsl:value-of select="@y"/>
										</xsl:attribute>
									</line>
								</xsl:for-each>
								<xsl:for-each select="impulsright/imp">
									<line stroke-width="0.5" stroke="darkgray" fill="none" marker-end="url(#b-circle)">
										<xsl:attribute name="x1">
											<xsl:value-of select="../@x1" />
										</xsl:attribute>
										<xsl:attribute name="x2">
											<xsl:value-of select="@x2" />
										</xsl:attribute>
										<xsl:attribute name="y1">
											<xsl:value-of select="@y" />
										</xsl:attribute>
										<xsl:attribute name="y2">
											<xsl:value-of select="@y"/>
										</xsl:attribute>
									</line>
									<line stroke-width="0.5" stroke="darkgray" fill="none" marker-end="url(#b-circle)">
										<xsl:attribute name="x1">
											<xsl:value-of select="../@x3" />
										</xsl:attribute>
										<xsl:attribute name="x2">
											<xsl:value-of select="@x4" />
										</xsl:attribute>
										<xsl:attribute name="y1">
											<xsl:value-of select="@y" />
										</xsl:attribute>
										<xsl:attribute name="y2">
											<xsl:value-of select="@y"/>
										</xsl:attribute>
									</line>
								</xsl:for-each>

								<xsl:for-each select = "digressions/m">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/otst">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/step">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/otkl">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/len">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/count">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/ogrsk">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/mark">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/R">
									<text fill="dimgray" font-size="11px" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
										<xsl:attribute name = "font-weight" >
											<xsl:value-of select = "@fw"/>
										</xsl:attribute>
										<xsl:value-of select = "@note"/>
									</text>
								</xsl:for-each>
								<xsl:for-each select = "digressions/rect">
									<rect width="136" height="10" style="stroke-width:0.5;fill:none;stroke: darkgray;">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@top"/>
										</xsl:attribute>
										<xsl:attribute name = "x" >
											<xsl:value-of select = "@x"/>
										</xsl:attribute>
									</rect>
								</xsl:for-each>
								<xsl:for-each select = "digressions/line">
									<line stroke-dasharray="0.25,0.25"  stroke="grey" fill="none">
										<xsl:attribute name="x1">
											<xsl:value-of select="@x" />
										</xsl:attribute>
										<xsl:attribute name="x2">
											<xsl:value-of select="@x" />
										</xsl:attribute>
										<xsl:attribute name="y1">
											<xsl:value-of select="@y1" />
										</xsl:attribute>
										<xsl:attribute name="y2">
											<xsl:value-of select="@y2"/>
										</xsl:attribute>
										<xsl:attribute name="stroke-width">
											<xsl:value-of select="@w" />
										</xsl:attribute>
									</line>
								</xsl:for-each>
								<xsl:for-each select = "speedline">
									<text fill="dimgray" font-size="11px" x="0" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@y1"/>
										</xsl:attribute>
										<xsl:value-of select = "@note1"/>
									</text>
									<text fill="dimgray" font-size="11px" x="0" style="white-space:pre">
										<xsl:attribute name = "y" >
											<xsl:value-of select = "@y2"/>
										</xsl:attribute>
										<xsl:value-of select = "@note2"/>
									</text>
									<polyline stroke-dasharray="0.25,1"  stroke="grey" stroke-width="1" fill="none">
										<xsl:attribute name="points">
											<xsl:value-of select="@points" />
										</xsl:attribute>
									</polyline>
									<rect width="175" height="21" style="stroke-width:0.5;fill:none;stroke: darkgray;" x="-2">
										<xsl:attribute name="y">
											<xsl:value-of select="@y3"/>
										</xsl:attribute>
									</rect>

								</xsl:for-each>

								<!-- <text x="0" font-size="8" y="31.5">
                                    <tspan x="5">м</tspan>
                                    <tspan x="18">| Отст</tspan>
                                    <tspan x="55">| Ст</tspan>
                                    <tspan x="70">| Откл</tspan>
                                    <tspan x="95">|Дл.</tspan>
                                    <tspan x="110">|Кол</tspan>
                                    <tspan x="135">|Огр.ск.</tspan>
                                    <tspan x="175">|</tspan>
                                    <tspan x="180">Зазор.л.</tspan>
                                    <tspan x="220">|</tspan>
                                    <tspan x="225">Зазор.пр.</tspan>
                                    <tspan x="264">|</tspan>
                                    <tspan x="272">Из.Б.л.</tspan>
                                    <tspan x="320">|</tspan>
                                    <tspan x="328">Из.Б.пр.</tspan>
                                    <tspan x="363">|</tspan>
                                    <tspan x="381">Нпк. л.</tspan>
                                    <tspan x="422">|</tspan>
                                    <tspan x="436">Нпк. пр.</tspan>
                                    <tspan x="480">|</tspan>
                                    <tspan x="495">Пу. л.</tspan>
                                    <tspan x="535">|</tspan>
                                    <tspan x="550">Пу. пр.</tspan>
                                    <tspan x="592">|</tspan>
                                    <tspan x="614">Отжатие</tspan>
                                    <tspan x="667">|</tspan>
                                    <tspan x="679">Шаблон</tspan>
                                    <tspan x="730">| Км</tspan>
                                </text> -->
								<text x="0" font-size="8" y="31.5">
									<tspan x="5">м</tspan>
									<tspan x="18">| Отст</tspan>
									<tspan x="55">| Ст</tspan>
									<tspan x="70">| Откл</tspan>
									<tspan x="100">|Дл.</tspan>
									<tspan x="120">|Кол</tspan>
									<tspan x="140">|Балл</tspan>
									<tspan x="170">| Огр.ск.</tspan>
									<tspan x="235">|</tspan>
									<tspan x="246">Неров. проф.</tspan>
									<tspan x="297">|</tspan>
									<tspan x="320">Зазор.лв.</tspan>
									<tspan x="368">|</tspan>
									<tspan x="390">Зазор пр.</tspan>
									<tspan x="437">|</tspan>
									<tspan x="470">Износ</tspan>
									<tspan x="515">|</tspan>
									<tspan x="535">Отжатие</tspan>
									<tspan x="585">|</tspan>
									<tspan x="610">Шаблон</tspan>
									<tspan x="660">|</tspan>
									<tspan x="685">План</tspan>
									<tspan x="730">| Км</tspan>
								</text>

								<rect x="0" height="12" width="770" style="fill: none;stroke: black;">
									<xsl:attribute name="y">
										<xsl:value-of select="@prer"/>
									</xsl:attribute>
								</rect>
							</svg>
						</div>

					</div>
				</xsl:for-each>
			</body>
		</html>
	</xsl:template>
</xsl:stylesheet>