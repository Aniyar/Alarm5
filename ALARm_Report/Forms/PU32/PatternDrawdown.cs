﻿
using ALARm.Core;
using ALARm.Services;
using ALARm.Core.AdditionalParameteres;
using ALARm.Core.Report;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using ALARm_Report.controls;

namespace ALARm_Report.Forms
{
    /// <summary>
    /// График диаграммы ЦНИИ-2 Уровень
    /// </summary>
    public class PatternDrawdown : ALARm.Core.Report.GraphicDiagrams
    {
        public static int c1520 = 25;
        public static float koef_gauge = 1.44f;
        public static float koef_gauge_mash = 2.81f;
        public static float koef_dropdown_move_left = 52.9f;
        public static float koef_dropdown_mash = 2.749f;
        public static float koef_dropdown_move_right = koef_dropdown_move_left - 10;
        public static float dropdaown_rule_step = 3.6f;
        public static float koef_straigthening_mash = 3.2f;
        public static float koef_straightening_move_left = 160.4f;
        public static float koef_straightening_move_right = 143.4f;
        public static float straightening_rule_step = 10.95f;
        public static float level_0 = 100;
        public static float level_rule_step = 5.357f;

        public const float left_straightening_side = 3;
        public const float right_straightening_side = 7.5f;
        public const float artificial_right = 8.5f;
        public const float artificial_left = 2;
        public const float ties_center = 5.25f;

        private float GaugeKoef = 1f;
        private float ProsKoef = 1f;


        private float GaugePosition = -2.8f;

        private float ProsRightPosition = 123;

        private float ProsLeftPosition = 72f;

        public static float koef_level_mash = 3.8f;
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            this.MainTrackStructureRepository = MainTrackStructureService.GetRepository();
            this.RdStructureRepository = RdStructureService.GetRepository();
            this.AdmStructureRepository = AdmStructureService.GetRepository();

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
                XDocument xdReport = new XDocument();
                XElement report = new XElement("report");

                var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;

                var tripProcesses = RdStructureService.GetTripsOnDistance(parentId, period);
                if (tripProcesses.Count == 0)
                {
                    MessageBox.Show(Properties.Resources.paramDataMissing);
                    return;
                }
                int svgIndex = template.Xsl.IndexOf("</svg>");
                template.Xsl = template.Xsl.Insert(svgIndex, RighstSideXslt());
                foreach (var tripProcess in tripProcesses)
                {
                    foreach (var track_id in admTracksId)
                    {
                        var trackName = AdmStructureService.GetTrackName(track_id);

                        tripProcess.Track_Id = track_id;

                        var trip = RdStructureService.GetTrip(tripProcess.Id);
                        var kilometers = RdStructureService.GetKilometersByTrip(trip);
                        kilometers = kilometers.Where(o => o.Track_id == track_id).ToList();
                        if (kilometers.Count == 0) continue;

                        ////Выбор километров по проезду-----------------
                        var filterForm = new FilterForm();
                        var filters = new List<Filter>();

                        var lkm = kilometers.Select(o => o.Number).ToList();

                        var roadName = AdmStructureService.GetRoadName(parentId, AdmStructureConst.AdmDistance, true);
                        filters.Add(new FloatFilter() { Name = "Начало (км)", Value = lkm.Min() });
                        filters.Add(new FloatFilter() { Name = "Конец (км)", Value = lkm.Max() });

                        filterForm.SetDataSource(filters);
                        if (filterForm.ShowDialog() == DialogResult.Cancel)
                            return;

                        kilometers = kilometers.Where(Km => ((float)(float)filters[0].Value <= Km.Number && Km.Number <= (float)(float)filters[1].Value)).ToList();
                        kilometers = (tripProcess.Travel_Direction == Direction.Reverse ? kilometers.OrderBy(o => o.Number) : kilometers.OrderByDescending(o => o.Number)).ToList();
                        if (kilometers.Count == 0) continue;
                        //--------------------------------------------

                        progressBar.Maximum = kilometers.Count;
                        var TripKms = RdStructureService.GetKilometersByTrip(trip);

                        foreach (var kilometer in kilometers)
                        {
                            XElement kmlist = new XElement("kmlist");

                            progressBar.Value = kilometers.IndexOf(kilometer) + 1;

                            var ind = TripKms.Where(o => o.Number == kilometer.Number).ToList();
                            int prevIndex = ind.Any() ? TripKms.IndexOf(ind.First()) : 0;

                            List<double> prevLevelAvgPart = new List<double>();
                            List<double> nextLevelAvgPart = new List<double>();
                            List<double> prevStrightAvgPart = new List<double>();
                            List<double> nextStrightAvgPart = new List<double>();

                            int n = 200;
                            int prevN = 200;
                            if (prevIndex > 0)
                            {
                                var PrKm = TripKms[prevIndex - 1];
                                var outData1 = (List<OutData>)RdStructureService.GetNextOutDatas(PrKm.Start_Index - 1, PrKm.GetLength() - 1, PrKm.Trip.Id);
                                outData1 = outData1.Where(o => o.km == PrKm.Number).ToList();
                                PrKm.AddDataRange(outData1, PrKm);

                                n = PrKm.LevelAvg.Count > prevN ? prevN : PrKm.LevelAvg.Count;
                                prevLevelAvgPart = PrKm.LevelAvg.GetRange(PrKm.LevelAvg.Count - n - 1, n);
                                prevStrightAvgPart = PrKm.StrightAvg.GetRange(PrKm.StrightAvg.Count - n - 1, n);
                            }
                            try
                            {
                                var NxKm = TripKms[prevIndex + 1];
                                var outData2 = (List<OutData>)RdStructureService.GetNextOutDatas(NxKm.Start_Index - 1, NxKm.GetLength() - 1, NxKm.Trip.Id);
                                outData2 = outData2.Where(o => o.km == NxKm.Number).ToList();
                                NxKm.AddDataRange(outData2, NxKm);

                                n = NxKm.LevelAvg.Count > prevN ? prevN : NxKm.LevelAvg.Count;
                                nextLevelAvgPart = NxKm.LevelAvg.GetRange(0, n);
                                nextStrightAvgPart = NxKm.StrightAvg.GetRange(0, n);
                            }
                            catch { }

                            var outData = (List<OutData>)RdStructureService.GetNextOutDatas(kilometer.Start_Index - 1, kilometer.GetLength() - 1, kilometer.Trip.Id);
                            kilometer.AddDataRange(outData, kilometer);

                            //lvl avg data
                            var Curves = new List<NatureCurves> { };
                            var StrightAvgTrapezoid = kilometer.StrightAvg.GetTrapezoid(prevStrightAvgPart, nextStrightAvgPart, 4, ref Curves);
                            var LevelAvgTrapezoid = kilometer.LevelAvg.GetTrapezoid(prevLevelAvgPart, nextLevelAvgPart, 10, ref Curves);
                            LevelAvgTrapezoid.Add(LevelAvgTrapezoid[LevelAvgTrapezoid.Count - 1]);
                            StrightAvgTrapezoid.Add(StrightAvgTrapezoid[StrightAvgTrapezoid.Count - 1]);

                            //zero line data
                            kilometer.GetZeroLines(outData, tripProcess, MainTrackStructureService.GetRepository());
                            kilometer.LoadTrackPasport(MainTrackStructureRepository, tripProcess.Trip_date);
                            kilometer.LoadDigresions(RdStructureRepository, MainTrackStructureRepository, tripProcess);

                            var sector_station = MainTrackStructureService.GetSector(track_id, kilometer.Number, tripProcess.Trip_date);
                            var fragment = MainTrackStructureService.GetMtoObjectsByCoord(tripProcess.Trip_date, kilometer.Number, MainTrackStructureConst.Fragments, kilometer.Direction_name, $"{trackName}") as Fragment;
                            var pdbSection = MainTrackStructureService.GetMtoObjectsByCoord(tripProcess.Trip_date, kilometer.Number, MainTrackStructureConst.MtoPdbSection, kilometer.Direction_name, $"{trackName}") as List<PdbSection>;

                            int fourStepOgrCoun = 0, otherfourStepOgrCoun = 0;

                            //Линий уровня по середине------------------------------------
                            List<string> result = new List<string>();
                            string drawdownRight = string.Empty, drawdownLeft = string.Empty, gauge = string.Empty, zeroGauge = string.Empty,
                                zeroStraightening = string.Empty, averageStraighteningRight = string.Empty, straighteningRight = string.Empty,
                                zeroStraighteningLeft = string.Empty, averageStraighteningLeft = string.Empty, straighteningLeft = string.Empty;

                            for (int index = 0; index < kilometer.meter.Count - 1; index++)
                            {
                                int metre = -kilometer.meter[index + 1];

                                drawdownRight += MMToPixelChartString(kilometer.DrawdownLeft[index] * ProsKoef + ProsRightPosition) + "," + metre + " ";
                                drawdownLeft += MMToPixelChartString(kilometer.DrawdownRight[index] * ProsKoef + ProsLeftPosition) + "," + metre + " ";
                                gauge += MMToPixelChartString((kilometer.Gauge[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";
                                zeroGauge += MMToPixelChartString((kilometer.fsh0[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";
                            }
                            result.Add(drawdownRight);
                            result.Add(drawdownLeft);
                            result.Add(gauge);
                            result.Add(zeroGauge);


                            var linesElem = new XElement("lines");
                            foreach (var polyline in result)
                            {
                                linesElem.Add(new XElement("line", polyline));
                            }
                            kmlist.Add(linesElem);
                            //------------------------------------------------------------

                            var svgLength = kilometer.GetLength() < 1000 ? 1000 : kilometer.GetLength();
                            var xp = (-kilometer.Start_m - svgLength - 50) + (svgLength + 105) - 52;
                            var direction = AdmStructureRepository.GetDirectionByTrack(kilometer.Track_id);
                            var trackclasses = (List<TrackClass>)MainTrackStructureRepository.GetMtoObjectsByCoord(trip.Trip_date, kilometer.Number, MainTrackStructureConst.MtoTrackClass, kilometer.Track_id);

                            //   int fourStepOgrCoun = 0, otherfourStepOgrCoun = 0;


                            for (int i = 0; i < 2; i++)
                            {
                                //параметры для цнии-2
                                kilometer.Rep_type_cni = true;

                                XElement addParam = new XElement("addparam",
                                    new XAttribute("top-title",
                                            (direction != null ? $"{direction.Name} ({direction.Code})" : "Неизвестный") + " Путь: " +
                                            kilometer.Track_name + $" Класс: {(trackclasses.Any() ? trackclasses.First().Class_Id.ToString() : "-")} Км:" + kilometer.Number + " " +
                                            (kilometer.PdbSection.Count > 0 ? $" ПЧ-{kilometer.PdbSection[0].Distance}" : " ПЧ-") + " Уст: " + " " +
                                            (kilometer.Speeds.Count > 0 ? $"{kilometer.Speeds.First().Passenger}/{kilometer.Speeds.First().Freight}" : "-/-")),
                                        //new XAttribute("common", common),

                                        new XAttribute("right-title",
                                            copyright + ": " + "ПО " + softVersion + "  " +
                                            systemName + ":" + tripProcess.Car + "(" + tripProcess.Chief.Trim() + ") (БПД от " + MainTrackStructureRepository.GetModificationDate() + ") <" +
                                            (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].RoadAbbr : "НЕИЗВ") + ">" + "<" + kilometer.Passage_time.ToString("dd.MM.yyyy  HH:mm") + ">" +
                                            "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(tripProcess.Travel_Direction.ToString())) + ">" +
                                            "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(tripProcess.Car_Position.ToString())) + ">" +
                                            "<" + tripProcess.Trip_date.Month + "-" + tripProcess.Trip_date.Year
                                             +
                                            " " + (tripProcess.Trip_Type == TripType.Control ? "контр." : tripProcess.Trip_Type == TripType.Work ? "раб." : "доп.") +
                                            " Проезд:" +
                                            tripProcess.Trip_date.ToString("dd.MM.yyyy  HH:mm") + " " + " ЦНИИ-2>" + " Л: " + (i + 1) + ">"),

                                        new XAttribute("right-title2",
                                            copyright + ": " + "ПО " + softVersion + "  " +
                                            systemName + ":" + tripProcess.Car + "(" + tripProcess.Chief.Trim() + ") (БПД от " + MainTrackStructureRepository.GetModificationDate() + ") <" +
                                            (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].RoadAbbr : "НЕИЗВ") + ">" + "<" + kilometer.Passage_time.ToString("dd.MM.yyyy  HH:mm") + ">" +
                                            "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(tripProcess.Travel_Direction.ToString())) + ">" +
                                            "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(tripProcess.Car_Position.ToString())) + ">" +
                                            "<" + tripProcess.Trip_date.Month + "-" + tripProcess.Trip_date.Year +
                                            " " + (tripProcess.Trip_Type == TripType.Control ? "контр." : tripProcess.Trip_Type == TripType.Work ? "раб." : "доп.") +
                                            " Проезд:" +
                                            tripProcess.Trip_date.ToString("dd.MM.yyyy  HH:mm") + " " + " ЦНИИ-2>" + " Л: " + (i + 1) + ">"),

                                         new XAttribute("right-title2-x", (-kilometer.Start_m - svgLength - 50) / 2),
                                         new XAttribute("fragment1-y", (-kilometer.Start_m - svgLength - 50) / 2 - 10),


                                        new XAttribute("pre", xp + 18),
                                        new XAttribute("prer", xp + 10),
                                        new XAttribute("topr", -kilometer.Start_m - svgLength - 39),
                                        new XAttribute("topf", xp + 8),
                                        new XAttribute("topx", -kilometer.Start_m - svgLength),
                                        new XAttribute("topx1", -kilometer.Start_m - svgLength - 30),
                                        new XAttribute("topx2", -kilometer.Start_m - svgLength - 15 - 8),

                                    new XAttribute("fragment", (kilometer.StationSection != null && kilometer.StationSection.Count > 0 ? "Станция: " + kilometer.StationSection[0].Station : (kilometer.Sector != null ? kilometer.Sector.ToString() : "")) + " Км:" + kilometer.Number),

                                    new XAttribute("viewbox", $"-20 { (i == 0 ? (-kilometer.Start_m - svgLength - 50 - 26) : (-kilometer.Start_m - svgLength - 50) / 2)} 830 {(svgLength + 105) / 2}"),
                                    new XAttribute("minY", -kilometer.Start_m),
                                    new XAttribute("maxY", -kilometer.Final_m),

                                    RightSideChart(
                                        tripProcess.Trip_date,
                                        kilometer,
                                        kilometer.Track_id,
                                        new float[] { 109.2f, 146f, 152.5f, 155f, -592 }),

                                    new XElement("xgrid",
                                        new XElement("x", MMToPixelChartString(GaugePosition - 10 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey")),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition - 8 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "1512"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition - 8 * GaugeKoef - 0.5f)), 
                                        new XAttribute("x", xp + 10)),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition - 4 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey")),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "1520"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition - 0.5f)), 
                                        new XAttribute("x", xp + 10)),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition + 8 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "1528"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition + 8 * GaugeKoef - 0.5f)), 
                                        new XAttribute("x", xp + 10)),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition + 16 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "1536"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition + 16 * GaugeKoef - 0.5f)), 
                                        new XAttribute("x", xp + 10)),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition + 22 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "1542"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition + 22 * GaugeKoef - 0.5f)), 
                                        new XAttribute("x", xp + 10)),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition + 26 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey")),
                                        
                                        new XElement("x", MMToPixelChartString(GaugePosition + 28 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "1548"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition + 28 * GaugeKoef - 0.5f)), 
                                        new XAttribute("x", xp + 10)),


                                        new XElement("x", MMToPixelChartString(ProsRightPosition - 10 * ProsKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "  –10"), 
                                        new XAttribute("y", MMToPixelChartString(ProsRightPosition - 10 * ProsKoef - 0.5f)), 
                                        new XAttribute("x", xp + 8)),
                                        
                                        new XElement("x", MMToPixelChartString(ProsRightPosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "      0"), 
                                        new XAttribute("y", MMToPixelChartString(ProsRightPosition - 0.5f)), 
                                        new XAttribute("x", xp + 8)),
                                        
                                        new XElement("x", MMToPixelChartString(ProsRightPosition + 10 * ProsKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "    10"), 
                                        new XAttribute("y", MMToPixelChartString(ProsRightPosition + 10 * ProsKoef - 0.5f)), 
                                        new XAttribute("x", xp + 8)),


                                        
                                        new XElement("x", MMToPixelChartString(ProsLeftPosition - 10 * ProsKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "  –10"), 
                                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 10 * ProsKoef - 0.5f)), 
                                        new XAttribute("x", xp + 8)),
                                        
                                        new XElement("x", MMToPixelChartString(ProsLeftPosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "      0"), 
                                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 0.5f)), 
                                        new XAttribute("x", xp + 8)),
                                        
                                        new XElement("x", MMToPixelChartString(ProsLeftPosition + 10 * ProsKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "    10"), 
                                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition + 10 * ProsKoef - 0.5f)), 
                                        new XAttribute("x", xp + 8))
                                        )
                                    );

                                var digElemenets = new XElement("digressions");
                                List<int> usedTops = new List<int>();
                                List<int> speedmetres = new List<int>();


                                var gmeter = kilometer.Start_m.RoundTo10() + 10;


                                foreach (var picket in kilometer.Pickets)
                                {
                                    picket.WriteNotesToReport(
                                        kilometer,
                                        speedmetres,
                                        addParam,
                                        digElemenets,
                                        ProsRightPosition,
                                        ProsLeftPosition,
                                        900,
                                        900,
                                        GaugePosition,
                                        900,
                                        this,
                                        ref fourStepOgrCoun,
                                        ref otherfourStepOgrCoun);
                                }

                                addParam.Add(new XAttribute("common", kilometer.GetdigressionsCount));
                                var GetBedomost = new List<Bedemost> { };
                                try
                                {
                                    // 1степень санау
                                    GetBedomost = ((List<Bedemost>)RdStructureRepository.GetBedemost(kilometer.Trip.Id)).Where(o => o.Km == kilometer.Number).ToList();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("GetBedomost " + e.Message);
                                }
                                addParam.Add(
                                    new XAttribute("speedlimit",
                                    kilometer.CalcMainPoint() + " " + $"Кол.ст.- 1:{(GetBedomost.Any() ? GetBedomost.First().Type1 : 0)}; " + kilometer.GetdigressionsCount +
                                    "  Кол.огр." + fourStepOgrCoun + "/" + otherfourStepOgrCoun + " Огр. " + kilometer.SpeedLimit + $" Скор.{(int)kilometer.Speed.Average()}")
                                    );


                                addParam.Add(digElemenets, new XAttribute("Page", i));


                                addParam.Add(digElemenets);

                                kmlist.Add(addParam);
                            }
                            report.Add(kmlist);
                        }
                      
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

        private object MMToPixelChart1(float mm)
        {
            return ((widthInPixel / widthImMM * mm + xBegin) - 28).ToString().Replace(",", ".");
        }
    }
}
