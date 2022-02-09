﻿using ALARm.Core;
using ALARm.Core.AdditionalParameteres;
using ALARm.Core.Report;
using ALARm.Services;
using ALARm_Report.controls;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace ALARm_Report.Forms
{
    public class DeviationDegree2 : Report
    {
        public override void Process(Int64 parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            //Сделать выбор периода
            List<long> admTracksId = new List<long>();
            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();
                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;
                admTracksId = choiceForm.admTracksIDs;
            }
            XDocument htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                List<Curve> curves = (MainTrackStructureService.GetCurves(parentId, MainTrackStructureConst.MtoCurve) as List<Curve>).Where(c => c.Radius <= 1200).OrderBy(c => c.Start_Km * 1000 + c.Start_M).ToList();
                XDocument xdReport = new XDocument();

                var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
                var road = AdmStructureService.GetRoadName(distance.Id, AdmStructureConst.AdmDistance, true);


                distance.Name = distance.Name.Replace("ПЧ-", "");



                var tripProcesses = RdStructureService.GetMainParametersProcess(period, distance.Name);
                if (tripProcesses.Count == 0)
                {
                    MessageBox.Show(Properties.Resources.paramDataMissing);
                    return;
                }

                XElement report = new XElement("report");

                foreach (var tripProcess in tripProcesses)
                {
                    foreach (var track_id in admTracksId)
                    {
                        var trackName = AdmStructureService.GetTrackName(track_id);
                        var trip = RdStructureService.GetTrip(tripProcess.Id);
                        var kms = RdStructureService.GetKilometersByTrip(trip);
                        if (!kms.Any()) continue;

                        kms = kms.Where(o => o.Track_id == track_id).ToList();

                        trip.Track_Id = track_id;
                        var lkm = kms.Select(o => o.Number).ToList();

                        if (lkm.Count() == 0) continue;
                        List<Digression> notes = RdStructureService.GetDigressions(tripProcess.Trip_id, distance.Code, new int[] { 2 });
                        var curvesAdmUnits = AdmStructureService.GetCurvesAdmUnits(curves[0].Id) as List<CurvesAdmUnits>;

                        CurvesAdmUnits curvesAdmUnit = curvesAdmUnits.Any() ? curvesAdmUnits[0] : null;

                        //var kms = RdStructureService.GetKilometerTrip(tripProcess.Trip_id);
                        //if (kms.Count() == 0) continue;
                        var directName = AdditionalParametersService.DirectName(tripProcess.Id, (int)tripProcess.Direction);

                        XElement tripElem = new XElement("trip",
                             new XAttribute("version", $"{DateTime.Now} v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}"),
                            new XAttribute("date_statement", DateTime.Now.Date.ToShortDateString()),
                            new XAttribute("direction", curvesAdmUnit.Direction),
                            new XAttribute("km", lkm.Min() + "-" + lkm.Max()),
                            new XAttribute("track", curvesAdmUnit.Track),
                            new XAttribute("check", tripProcess.GetProcessTypeName),
                            new XAttribute("distance", distance.Code),
                            new XAttribute("periodDate", period.Period),
                            new XAttribute("road", road),

                            new XAttribute("chief", tripProcess.Chief),
                            new XAttribute("ps", tripProcess.Car));
                        XElement Direct = new XElement("direction",
                           new XAttribute("name",
                               tripProcess.DirectionName + " (" + tripProcess.DirectionCode + "), Путь: " + trackName + ", ПЧ: " + distance.Code)
                       );

                        var ListS3 = RdStructureService.GetS3(tripProcess.Trip_id, 2, distance.Name) as List<S3>;

                        //ListS3 = ListS3.Where(o => o.Km >= 137 && o.Km <= 147).ToList();

                        //Участки дист коррекция
                        var dist_section = MainTrackStructureService.GetDistSectionByDistId(distance.Id);
                        foreach (var item in ListS3)
                        {
                            var ds = dist_section.Where(
                                o => item.Km * 1000 + item.Meter >= o.Start_Km * 1000 + o.Start_M && item.Km * 1000 + item.Meter <= o.Final_Km * 1000 + o.Final_M).ToList();

                            item.Pchu = ds.First().Pchu;
                            item.Pd = ds.First().Pd;
                            item.Pdb = ds.First().Pdb;
                        }

                        string lastDirection = String.Empty, lastTrack = String.Empty;
                        int lastPchu = -1, lastPd = -1, lastPdb = -1, lastKm = -1;
                        List<DigressionTotal> totals = new List<DigressionTotal>();
                        DigressionTotal digressionTotal = new DigressionTotal();
                        XElement xeDirection = new XElement("directions");
                        XElement xeTracks = new XElement("tracks");

                        int constrictionCount = 0; //Суж
                        int broadeningCount = 0; //Уш
                        int levelCount = 0; //У
                        int skewnessCount = 0; //П - просадка
                        int drawdownCount = 0; //Пр - перекос
                        int straighteningCount = 0; //Р

                        int PUTconstrictionCount = 0; //Суж
                        int PUTbroadeningCount = 0; //Уш
                        int PUTlevelCount = 0; //У
                        int PUTskewnessCount = 0; //П - просадка
                        int PUTdrawdownCount = 0; //Пр - перекос
                        int PUTstraighteningCount = 0; //Р

                        foreach (var s3 in ListS3)
                        {
                            XElement Notes = new XElement("Note");

                            switch (s3.Ots)
                            {
                                case "Пр.п":
                                case "Пр.л":
                                    drawdownCount += s3.Kol;
                                    break;
                                case "Суж":
                                    s3.Kol = s3.Len / 4 + (s3.Len % 4 + 1 > 1 ? 1 : 0);
                                    constrictionCount += s3.Kol;

                                    break;
                                case "Уш":
                                    s3.Kol = s3.Len / 4 + (s3.Len % 4 + 1 > 1 ? 1 : 0);
                                    broadeningCount += s3.Kol;
                                    break;
                                case "У":
                                    s3.Kol = s3.Len / 10 + (s3.Len % 10 + 1 > 1 ? 1 : 0);
                                    levelCount += s3.Kol;
                                    break;
                                case "П":
                                    skewnessCount += s3.Kol;
                                    break;
                                case "Р":
                                    straighteningCount += s3.Kol;
                                    break;
                            }
                            if (s3.Ots.Equals("Анп") || s3.Ots.Equals("ПрУ") || s3.Ots.Equals("?Анп"))
                            {
                                continue;
                            }
                            else
                            {
                                if (s3.Naprav.Equals(lastDirection))
                                {
                                    if (s3.Put.Equals(lastTrack))
                                    {
                                        switch (s3.Ots)
                                        {
                                            case "Пр.п":
                                            case "Пр.л":
                                                PUTdrawdownCount += s3.Kol;
                                                break;
                                            case "Суж":
                                                PUTconstrictionCount += s3.Kol;
                                                break;
                                            case "Уш":
                                                PUTbroadeningCount += s3.Kol;
                                                break;
                                            case "У":
                                                PUTlevelCount += s3.Kol;
                                                break;
                                            case "П":
                                                PUTskewnessCount += s3.Kol;
                                                break;
                                            case "Р":
                                                PUTstraighteningCount += s3.Kol;
                                                break;
                                        }

                                        if (s3.Pchu == lastPchu)
                                        {
                                            if (s3.Pd == lastPd)
                                            {
                                                if (s3.Pdb == lastPdb)
                                                {
                                                    if (s3.Km == lastKm)
                                                    {

                                                        XElement xeNote = new XElement("note",
                                                            new XAttribute("pchu", ""),
                                                            new XAttribute("pd", ""),
                                                            new XAttribute("pdb", ""),
                                                            new XAttribute("km", ""),
                                                            new XAttribute("m", s3.Meter),
                                                            new XAttribute("deviation", s3.Ots),
                                                            new XAttribute("digression", s3.Otkl),
                                                            new XAttribute("len", s3.Len),
                                                            new XAttribute("count", s3.Kol));

                                                        if (totals.Any(t => t.Name == s3.Ots))
                                                        {
                                                            totals.Where(t => t.Name == s3.Ots).First().Count += 1;
                                                        }
                                                        else
                                                        {
                                                            digressionTotal.Name = s3.Ots;
                                                            digressionTotal.Count = 1;
                                                            totals.Add(digressionTotal);
                                                        }

                                                        xeTracks.Add(xeNote);
                                                    }
                                                    else
                                                    {
                                                        lastKm = s3.Km;

                                                        XElement xeNote = new XElement("note",
                                                            new XAttribute("pchu", ""),
                                                            new XAttribute("pd", ""),
                                                            new XAttribute("pdb", ""),
                                                            new XAttribute("km", s3.Km),
                                                            new XAttribute("m", s3.Meter),
                                                            new XAttribute("deviation", s3.Ots),
                                                            new XAttribute("digression", s3.Otkl),
                                                            new XAttribute("len", s3.Len),
                                                            new XAttribute("count", s3.Kol));

                                                        if (totals.Any(t => t.Name == s3.Ots))
                                                        {
                                                            totals.Where(t => t.Name == s3.Ots).First().Count += 1;
                                                        }
                                                        else
                                                        {
                                                            digressionTotal.Name = s3.Ots;
                                                            digressionTotal.Count = 1;
                                                            totals.Add(digressionTotal);
                                                        }

                                                        xeTracks.Add(xeNote);
                                                    }
                                                }
                                                else
                                                {
                                                    lastPdb = s3.Pdb;
                                                    lastKm = s3.Km;

                                                    XElement xeNote = new XElement("note",
                                                        new XAttribute("pchu", ""),
                                                        new XAttribute("pd", ""),
                                                        new XAttribute("pdb", s3.Pdb),
                                                        new XAttribute("km", s3.Km),
                                                        new XAttribute("m", s3.Meter),
                                                        new XAttribute("deviation", s3.Ots),
                                                        new XAttribute("digression", s3.Otkl),
                                                        new XAttribute("len", s3.Len),
                                                        new XAttribute("count", s3.Kol));

                                                    if (totals.Any(t => t.Name == s3.Ots))
                                                    {
                                                        totals.Where(t => t.Name == s3.Ots).First().Count += 1;
                                                    }
                                                    else
                                                    {
                                                        digressionTotal.Name = s3.Ots;
                                                        digressionTotal.Count = 1;
                                                        totals.Add(digressionTotal);
                                                    }

                                                    xeTracks.Add(xeNote);
                                                }
                                            }
                                            else
                                            {
                                                lastPd = s3.Pd;
                                                lastPdb = s3.Pdb;
                                                lastKm = s3.Km;

                                                XElement xeNote = new XElement("note",
                                                    new XAttribute("pchu", ""),
                                                    new XAttribute("pd", s3.Pd),
                                                    new XAttribute("pdb", s3.Pdb),
                                                    new XAttribute("km", s3.Km),
                                                    new XAttribute("m", s3.Meter),
                                                    new XAttribute("deviation", s3.Ots),
                                                    new XAttribute("digression", s3.Otkl),
                                                    new XAttribute("len", s3.Len),
                                                    new XAttribute("count", s3.Kol));

                                                if (totals.Any(t => t.Name == s3.Ots))
                                                {
                                                    totals.Where(t => t.Name == s3.Ots).First().Count += 1;
                                                }
                                                else
                                                {
                                                    digressionTotal.Name = s3.Ots;
                                                    digressionTotal.Count = 1;
                                                    totals.Add(digressionTotal);
                                                }

                                                xeTracks.Add(xeNote);
                                            }
                                        }
                                        else
                                        {
                                            lastPchu = s3.Pchu;
                                            lastPd = s3.Pd;
                                            lastPdb = s3.Pdb;
                                            lastKm = s3.Km;

                                            XElement xeNote = new XElement("note",
                                                new XAttribute("pchu", s3.Pchu),
                                                new XAttribute("pd", s3.Pd),
                                                new XAttribute("pdb", s3.Pdb),
                                                new XAttribute("km", s3.Km),
                                                new XAttribute("m", s3.Meter),
                                                new XAttribute("deviation", s3.Ots),
                                                new XAttribute("digression", s3.Otkl),
                                                new XAttribute("len", s3.Len),
                                                new XAttribute("count", s3.Kol));

                                            if (totals.Any(t => t.Name == s3.Ots))
                                            {
                                                totals.Where(t => t.Name == s3.Ots).First().Count += 1;
                                            }
                                            else
                                            {
                                                digressionTotal.Name = s3.Ots;
                                                digressionTotal.Count = 1;
                                                totals.Add(digressionTotal);
                                            }

                                            xeTracks.Add(xeNote);
                                        }
                                    }
                                    else
                                    {


                                        if (!lastTrack.Equals(String.Empty))
                                        {
                                            xeDirection.Add(xeTracks);
                                        }
                                        xeTracks = new XElement("tracks",
                                            new XAttribute("track", s3.Put));



                                        lastTrack = s3.Put;
                                        lastPchu = s3.Pchu;
                                        lastPd = s3.Pd;
                                        lastPdb = s3.Pdb;
                                        lastKm = s3.Km;

                                        XElement xeNote = new XElement("note",
                                            new XAttribute("pchu", s3.Pchu),
                                            new XAttribute("pd", s3.Pd),
                                            new XAttribute("pdb", s3.Pdb),
                                            new XAttribute("km", s3.Km),
                                            new XAttribute("m", s3.Meter),
                                            new XAttribute("deviation", s3.Ots),
                                            new XAttribute("digression", s3.Otkl),
                                            new XAttribute("len", s3.Len),
                                            new XAttribute("count", s3.Kol));

                                        if (totals.Any(t => t.Name == s3.Ots))
                                        {
                                            totals.Where(t => t.Name == s3.Ots).First().Count += 1;
                                        }
                                        else
                                        {
                                            digressionTotal.Name = s3.Ots;
                                            digressionTotal.Count = 1;
                                            totals.Add(digressionTotal);
                                        }

                                        xeTracks.Add(xeNote);
                                    }
                                }
                                else
                                {
                                    if (!lastDirection.Equals(String.Empty))
                                    {
                                        xeDirection.Add(xeTracks);
                                        tripElem.Add(xeDirection);
                                    }
                                    xeDirection = new XElement("directions",
                                        new XAttribute("direction", s3.Direction_full),
                                          new XAttribute("track", s3.Put)
                                          );
                                    xeTracks = new XElement("tracks",
                                        new XAttribute("track", s3.Put)
                                        );
                                    //,
                                    //new XAttribute("countPUT", PUTconstrictionCount + PUTbroadeningCount + PUTlevelCount + PUTskewnessCount + PUTdrawdownCount + PUTstraighteningCount)


                                    lastDirection = s3.Naprav;
                                    lastTrack = s3.Put;
                                    lastPchu = s3.Pchu;
                                    lastPd = s3.Pd;
                                    lastPdb = s3.Pdb;
                                    lastKm = s3.Km;

                                    XElement xeNote = new XElement("note",
                                       new XAttribute("pchu", s3.Pchu),
                                        new XAttribute("pd", s3.Pd),
                                        new XAttribute("pdb", s3.Pdb),
                                        new XAttribute("km", s3.Km),
                                        new XAttribute("m", s3.Meter),
                                        new XAttribute("deviation", s3.Ots),
                                        new XAttribute("digression", s3.Otkl),
                                        new XAttribute("len", s3.Len),
                                        new XAttribute("count", s3.Kol));

                                    if (totals.Any(t => t.Name == s3.Ots))
                                    {
                                        totals.Where(t => t.Name == s3.Ots).First().Count += 1;
                                    }
                                    else
                                    {
                                        digressionTotal.Name = s3.Ots;
                                        digressionTotal.Count = 1;
                                        totals.Add(digressionTotal);
                                    }

                                    xeTracks.Add(xeNote);
                                }
                            }
                        }

                        //В том числе:
                        if (drawdownCount > 0)
                        {
                            tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Пр - " + drawdownCount)));
                        }
                        if (constrictionCount > 0)
                        {
                            tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Суж - " + constrictionCount)));
                        }
                        if (broadeningCount > 0)
                        {
                            tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Уш - " + broadeningCount)));
                        }
                        if (levelCount > 0)
                        {
                            tripElem.Add(new XElement("total", new XAttribute("totalinfo", "У - " + levelCount)));
                        }
                        if (skewnessCount > 0)
                        {
                            tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П - " + skewnessCount)));
                        }
                        if (straighteningCount > 0)
                        {
                            tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Р - " + straighteningCount)));
                        }
                        tripElem.Add(new XAttribute("countDistance", drawdownCount + constrictionCount + broadeningCount + levelCount + skewnessCount + straighteningCount));
                        xeTracks.Add(new XAttribute("count", drawdownCount + constrictionCount + broadeningCount + levelCount + skewnessCount + straighteningCount));


                        xeDirection.Add(xeTracks);
                        tripElem.Add(xeDirection);
                        report.Add(tripElem);
                    }
                }
                xdReport.Add(report);
                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }
            try
            {
                htReport.Save(Path.GetTempPath() + "/report.html");
            }
            catch
            {
                MessageBox.Show("Ошибка сохранения файлы");
            }
            finally
            {
                System.Diagnostics.Process.Start(Path.GetTempPath() + "/report.html");
            }
        }
    }
}
