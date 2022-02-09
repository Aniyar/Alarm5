﻿using ALARm.Core;
using ALARm.Core.Report;
using ALARm.Services;
using ALARm_Report.controls;
using MetroFramework.Controls;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace ALARm_Report.Forms
{
    public class MainParametersGRKSpeedMove : ALARm.Core.Report.GraphicDiagrams
    {
        private readonly float LevelPosition = 32.5f;
        private readonly float LevelStep = 7.5f;
        private readonly float LevelKoef = 0.25f;

        private readonly float StraighRighttPosition = 62f;
        private readonly float StrightStep = 7.5f;
        private readonly float StrightKoef = 0.5f;

        private readonly float ProsKoef = 1f;

        private readonly float GaugeKoef = 0.5f;

        private readonly float StrightLeftPosition = 71f;

        private readonly float GaugePosition = 100.5f;

        private readonly float ProsRightPosition = 138.5f;

        private readonly float ProsLeftPosition = 124.5f;

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

            diagramName = "Дубликат";
            XDocument htReport = new XDocument();
            var svgLength = 0;

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

                foreach (var trip in tripProcesses)
                {
                    foreach (var track_id in admTracksId)
                    {
                        var trackName = AdmStructureService.GetTrackName(track_id);

                        trip.Track_Id = track_id;
                        var kilometers = RdStructureService.GetKilometersByTrip(trip);

                        kilometers = kilometers.Where(o => o.Track_id == track_id).ToList();
                        if (kilometers.Count == 0) continue;

                        ////Выбор километров по проезду-----------------
                        var filterForm = new FilterForm();
                        var filters = new List<Filter>();

                        var lkm = kilometers.Select(o => o.Number).ToList();

                        var roadName = AdmStructureService.GetRoadName(parentId, AdmStructureConst.AdmDistance, true);
                        filters.Add(new FloatFilter() { Name = "Начало (км)", Value = 145 });
                        filters.Add(new FloatFilter() { Name = "Конец (км)", Value = 145 });

                        filterForm.SetDataSource(filters);
                        if (filterForm.ShowDialog() == DialogResult.Cancel)
                            return;

                        kilometers = kilometers.Where(Km => ((float)(float)filters[0].Value <= Km.Number && Km.Number <= (float)(float)filters[1].Value)).ToList();
                        kilometers = (trip.Travel_Direction == Direction.Reverse ? kilometers.OrderBy(o => o.Number) : kilometers.OrderByDescending(o => o.Number)).ToList();
                        //--------------------------------------------

                        progressBar.Maximum = kilometers.Count;

                        var TripKms = RdStructureService.GetKilometersByTrip(trip);

                        foreach (var kilometer in kilometers)
                        {
                            XElement kmlist = new XElement("kmlist");

                            progressBar.Value = kilometers.IndexOf(kilometer) + 1;

                            var ind = TripKms.Where(o => o.Number == kilometer.Number).ToList();
                            int prevIndex = ind.Any() ? TripKms.IndexOf( ind.First() ): 0;

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
                            //outData = outData.Where(o => o.km == kilometer.Number).ToList();
                            kilometer.AddDataRange(outData, kilometer);

                            //lvl avg data
                            var Curves = new List<NatureCurves> { };
                            var StrightAvgTrapezoid = kilometer.StrightAvg.GetTrapezoid(prevStrightAvgPart, nextStrightAvgPart, 4, ref Curves);
                            var LevelAvgTrapezoid = kilometer.LevelAvg.GetTrapezoid(prevLevelAvgPart, nextLevelAvgPart, 10, ref Curves);
                            LevelAvgTrapezoid.Add(LevelAvgTrapezoid[LevelAvgTrapezoid.Count - 1]);
                            StrightAvgTrapezoid.Add(StrightAvgTrapezoid[StrightAvgTrapezoid.Count - 1]);

                            

                            //zero line data
                            kilometer.GetZeroLines(outData, trip, MainTrackStructureService.GetRepository());
                            kilometer.LoadTrackPasport(MainTrackStructureRepository, trip.Trip_date);
                            kilometer.LoadDigresions(RdStructureRepository, MainTrackStructureRepository, trip);

                            var sector_station = MainTrackStructureService.GetSector(track_id, kilometer.Number, trip.Trip_date);
                            var fragment = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, kilometer.Number, MainTrackStructureConst.Fragments, kilometer.Direction_name, $"{trackName}") as Fragment;
                            var pdbSection = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, kilometer.Number, MainTrackStructureConst.MtoPdbSection, kilometer.Direction_name, $"{trackName}") as List<PdbSection>;

                            //Линий уровня по середине------------------------------------
                            List<string> result = new List<string>();
                            string drawdownRight = string.Empty, drawdownLeft = string.Empty, gauge = string.Empty, zeroGauge = string.Empty,
                                    zeroStraightening = string.Empty, averageStraighteningRight = string.Empty, straighteningRight = string.Empty,
                                    zeroStraighteningLeft = string.Empty, averageStraighteningLeft = string.Empty, straighteningLeft = string.Empty,
                                    level = string.Empty, averageLevel = string.Empty, zeroLevel = string.Empty;

                            int fourStepOgrCoun = 0, otherfourStepOgrCoun = 0;


                            svgLength = kilometer.GetLength() < 1000 ? 1000 : kilometer.GetLength();
                            var xp = (-kilometer.Start_m - svgLength - 50) + (svgLength + 105) - 52;
                            var direction = AdmStructureRepository.GetDirectionByTrack(kilometer.Track_id);

                            XElement addParam = new XElement("addparam",
                            //    new XAttribute("top-title",
                            //        (direction != null ? $"{direction.Name} ({direction.Code} )" : "Неизвестный") + " Путь: " + kilometer.Track_name + " Км:" +
                            //        kilometer.Number + " " + (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].ToString() : " ПЧ-/ПЧУ-/ПД-/ПДБ-") + " Уст: " 
                            //        + " " + (kilometer.Speeds.Count > 0 ? $"сапс{ kilometer.Speeds.Last().Sapsan }/{ kilometer.Speeds.Last().Passenger }/{ kilometer.Speeds.Last().Freight }" : "-/-/-") 
                            //        + $" Скор:{(int)kilometer.Speed.Average()}"),

                                new XAttribute("top-title",
                                    (direction != null ? $"{direction.Name} ({direction.Code})" : "Неизвестный") + " Путь: " +
                                    kilometer.Track_name + " Км:" + kilometer.Number + " " +
                                    (kilometer.PdbSection.Count > 0 ? $" ПЧ-{kilometer.PdbSection[0].Distance}" : " ПЧ-") + " Уст: " + " " +
                                    (kilometer.Speeds.Count > 0 ? $"{kilometer.Speeds.First().Passenger}/{kilometer.Speeds.First().Freight}" : "-/-")),

                                new XAttribute("right-title",
                                    copyright + ": " + "ПО " + softVersion + "  " +
                                    systemName + ":" + trip.Car + "(" + trip.Chief.Trim() + ") (БПД от " + MainTrackStructureRepository.GetModificationDate() + ") <" + 
                                    (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].RoadAbbr : "НЕИЗВ") + ">" + "<" + kilometer.Passage_time.ToString("dd.MM.yyyy  HH:mm") + ">" +
                                    "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Travel_Direction.ToString())) + ">" +
                                    "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Car_Position.ToString())) + ">" +
                                    "<" + trip.Trip_date.Month + "-" + trip.Trip_date.Year + " " + (trip.Trip_Type == TripType.Control ? "контр." : trip.Trip_Type == TripType.Work ? "раб." : "доп.") + 
                                    " Проезд:" + trip.Trip_date.ToString("dd.MM.yyyy  HH:mm") + " " + diagramName + ">"
                                    ),

                                new XAttribute("pre", xp + 30),
                                new XAttribute("prer", xp + 21),
                                new XAttribute("topr", -kilometer.Start_m - svgLength - 45),
                                new XAttribute("topf", xp + 10),
                                new XAttribute("topx", -kilometer.Start_m - svgLength),
                                new XAttribute("topx1", -kilometer.Start_m - svgLength - 30),
                                new XAttribute("topx2", -kilometer.Start_m - svgLength - 15),
                                new XAttribute("fragment", (kilometer.StationSection != null && kilometer.StationSection.Count > 0 ? "Станция: " + kilometer.StationSection[0].Station : (kilometer.Sector != null ? kilometer.Sector.ToString() : "")) + " Км:" + kilometer.Number),
                                new XAttribute("viewbox", $"-20 {-kilometer.Start_m - svgLength - 50} 830 {svgLength + 105}"),
                                new XAttribute("minY", -kilometer.Start_m),
                                new XAttribute("maxY", -kilometer.Final_m),
                                new XAttribute("minYround", -(kilometer.Start_m - kilometer.Start_m % 100)),

                                RightSideChart(trip.Trip_date, kilometer, kilometer.Track_id, new float[] { 151f, 146f, 152.5f, 155f }),

                                new XElement("xgrid",
                                    new XElement("x", MMToPixelChartString(LevelPosition - LevelStep), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "  30"), 
                                        new XAttribute("y", MMToPixelChartString(LevelPosition - LevelStep - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(LevelPosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "      0"), 
                                        new XAttribute("y", MMToPixelChartString(LevelPosition - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(LevelPosition + LevelStep), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "    30"), 
                                        new XAttribute("y", MMToPixelChartString(LevelPosition + LevelStep - 0.5f)), 
                                        new XAttribute("x", xp + 15)),

                                    new XElement("x", MMToPixelChartString(StraighRighttPosition - StrightStep), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "  15"), 
                                        new XAttribute("y", MMToPixelChartString(StraighRighttPosition - StrightStep - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(StraighRighttPosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "      0"), 
                                        new XAttribute("y", MMToPixelChartString(StraighRighttPosition - 1f)), 
                                        new XAttribute("x", xp + 15)),
                                    //new XElement("x", MMToPixelChartString(StraighRighttPosition + StrightStep / 10f), 
                                    //    new XAttribute("dasharray", "0.5,2"), 
                                    //    new XAttribute("stroke", "grey"), 
                                    //    new XAttribute("label", "      15"), 
                                    //    new XAttribute("y", MMToPixelChartString(StraighRighttPosition + StrightStep / 10f + 0.2f)), 
                                    //    new XAttribute("x", xp + 15)),
                                    //new XElement("x", MMToPixelChartString(StrightLeftPosition - StrightStep / 10f), 
                                    //    new XAttribute("dasharray", "0.5,2"), 
                                    //    new XAttribute("stroke", "grey"), 
                                    //    new XAttribute("label", "    15"), 
                                    //    new XAttribute("y", MMToPixelChartString(StrightLeftPosition - StrightStep / 10f - 1f)), 
                                    //    new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(StrightLeftPosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "      0"), 
                                        new XAttribute("y", MMToPixelChartString(StrightLeftPosition + 0.2f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(StrightLeftPosition + StrightStep), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "    15"), 
                                        new XAttribute("y", MMToPixelChartString(StrightLeftPosition + StrightStep - 0.5f)), 
                                        new XAttribute("x", xp + 15)),

                                    //new XElement("x", MMToPixelChartString(GaugePosition - 10 * GaugeKoef), 
                                    //    new XAttribute("dasharray", "0.5,2"), 
                                    //    new XAttribute("stroke", "grey")),

                                    new XElement("x", MMToPixelChartString(GaugePosition - 16 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "1512"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition - 16 * GaugeKoef - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    //new XElement("x", MMToPixelChartString(GaugePosition - 4 * GaugeKoef), 
                                    //    new XAttribute("dasharray", "0.5,2"), 
                                    //    new XAttribute("stroke", "grey")),
                                    new XElement("x", MMToPixelChartString(GaugePosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "1520"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(GaugePosition + 16 * GaugeKoef), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "1528"), 
                                        new XAttribute("y", MMToPixelChartString(GaugePosition + 16 * GaugeKoef - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(GaugePosition + 32 * GaugeKoef),
                                        new XAttribute("dasharray", "0.5,2"),
                                        new XAttribute("stroke", "grey"),
                                        new XAttribute("label", "1536"),
                                        new XAttribute("y", MMToPixelChartString(GaugePosition + 32 * GaugeKoef - 0.5f)),
                                        new XAttribute("x", xp + 15)),
                                    //new XElement("x", MMToPixelChartString(GaugePosition + 22 * GaugeKoef), 
                                    //    new XAttribute("dasharray", "0.5,2"),
                                    //    new XAttribute("stroke", "grey"), 
                                    //    new XAttribute("label", "1542"), 
                                    //    new XAttribute("y", MMToPixelChartString(GaugePosition + 22 * GaugeKoef - 0.5f)), 
                                    //    new XAttribute("x", xp + 15)),
                                    //new XElement("x", MMToPixelChartString(GaugePosition + 26 * GaugeKoef), 
                                    //    new XAttribute("dasharray", "0.5,2"), 
                                    //    new XAttribute("stroke", "grey")),
                                    //new XElement("x", MMToPixelChartString(GaugePosition + 28 * GaugeKoef),
                                    //    new XAttribute("dasharray", "0.5,2"), 
                                    //    new XAttribute("stroke", "grey"), 
                                    //    new XAttribute("label", "1548"), 
                                    //    new XAttribute("y", MMToPixelChartString(GaugePosition + 28 * GaugeKoef - 0.5f)), 
                                    //    new XAttribute("x", xp + 15)),

                                    new XElement("x", MMToPixelChartString(ProsRightPosition - 5), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "  –5"), 
                                        new XAttribute("y", MMToPixelChartString(ProsRightPosition - 5 - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(ProsRightPosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "      0"), 
                                        new XAttribute("y", MMToPixelChartString(ProsRightPosition - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(ProsRightPosition + 5), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "    5"), 
                                        new XAttribute("y", MMToPixelChartString(ProsRightPosition + 5 - 0.5f)), 
                                        new XAttribute("x", xp + 15)),

                                    new XElement("x", MMToPixelChartString(ProsLeftPosition - 5), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "  –5"), 
                                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 5 - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(ProsLeftPosition), 
                                        new XAttribute("dasharray", "3,3"), 
                                        new XAttribute("stroke", "black"), 
                                        new XAttribute("label", "      0"), 
                                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 0.5f)), 
                                        new XAttribute("x", xp + 15)),
                                    new XElement("x", MMToPixelChartString(ProsLeftPosition + 5), 
                                        new XAttribute("dasharray", "0.5,2"), 
                                        new XAttribute("stroke", "grey"), 
                                        new XAttribute("label", "    5"), 
                                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition + 5 - 0.5f)), 
                                        new XAttribute("x", xp + 15))
                                    ));

                            var KoefSpeed = 2.0;
                            for (int index = 0; index < kilometer.meter.Count - 1; index++)
                            {
                                int metre = -kilometer.meter[index];

                                drawdownRight += MMToPixelChartString(kilometer.DrawdownLeft[index] * ProsKoef + ProsRightPosition) + "," + metre + " ";
                                drawdownLeft += MMToPixelChartString(kilometer.DrawdownRight[index] * ProsKoef + ProsLeftPosition) + "," + metre + " ";
                                gauge += MMToPixelChartString((kilometer.Gauge[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";
                                zeroGauge += MMToPixelChartString((kilometer.fsh0[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";

                                zeroStraightening +=      MMToPixelChartString(kilometer.fZeroStright[index] * StrightKoef + StraighRighttPosition) + "," + metre + " ";
                                averageStraighteningRight += MMToPixelChartString(StrightAvgTrapezoid[index] * StrightKoef + StraighRighttPosition) + "," + metre + " ";
                                var drh = StrightAvgTrapezoid[index] + (kilometer.StrightRight[index] - StrightAvgTrapezoid[index]);
                                straighteningRight += MMToPixelChartString(drh * StrightKoef + StraighRighttPosition) + "," + metre + " ";

                                zeroStraighteningLeft += MMToPixelChartString(kilometer.fZeroStright[index] * StrightKoef + StrightLeftPosition) + "," + metre + " ";
                                averageStraighteningLeft += MMToPixelChartString(StrightAvgTrapezoid[index] * StrightKoef + StrightLeftPosition) + "," + metre + " ";
                                drh = StrightAvgTrapezoid[index]  + (kilometer.StrightLeft[index] - StrightAvgTrapezoid[index]);
                                straighteningLeft += MMToPixelChartString(drh * StrightKoef + StrightLeftPosition) + "," + metre + " ";

                                level += MMToPixelChartString(kilometer.Level[index] * LevelKoef + LevelPosition) + "," + metre + " ";
                                averageLevel += MMToPixelChartString(LevelAvgTrapezoid[index] * LevelKoef + LevelPosition) + "," + metre + " ";
                                zeroLevel += MMToPixelChartString(kilometer.flvl0[index] * LevelKoef + LevelPosition) + "," + metre + " ";
                            }
                            var style = "fill:none;stroke:dimgray;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:0.3";
                            var styleAverage = "fill:none;stroke:dimgray;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:0.7; stroke-dasharray:0.7 0.6;";


                            addParam.Add(new XElement("polyline", new XAttribute("points", drawdownRight), new XAttribute("style", style)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", drawdownLeft), new XAttribute("style", style)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", gauge), new XAttribute("style", style)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", zeroGauge), new XAttribute("style", style)));

                            addParam.Add(new XElement("polyline", new XAttribute("points", zeroStraightening), new XAttribute("style", style)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", averageStraighteningRight), new XAttribute("style", styleAverage)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", straighteningRight), new XAttribute("style", style)));

                            addParam.Add(new XElement("polyline", new XAttribute("points", zeroStraighteningLeft), new XAttribute("style", style)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", averageStraighteningLeft), new XAttribute("style", styleAverage)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", straighteningLeft), new XAttribute("style", style)));

                            addParam.Add(new XElement("polyline", new XAttribute("points", level), new XAttribute("style", style)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", averageLevel), new XAttribute("style", styleAverage)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", zeroLevel), new XAttribute("style", style)));

                            char separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];

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
                                    StraighRighttPosition,
                                    StrightLeftPosition,
                                    GaugePosition,
                                    LevelPosition,
                                    this,
                                    ref fourStepOgrCoun,
                                    ref otherfourStepOgrCoun);
                            }

                            //addParam.Add(new XAttribute("common", kilometer.GetdigressionsCount));
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



                            addParam.Add(digElemenets);
                            report.Add(addParam);
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
                htReport.Save(Path.GetTempPath() + "/1report.html");

                var svg = htReport.Element("html").Element("body").Element("div").Element("div").Element("svg");
                var svgDoc = SvgDocument.FromSvg<SvgDocument>(svg.ToString());
                svgDoc.Width = 830 * 3;
                svgDoc.Height = (svgLength + 105) * 3;
            }
            catch
            {
                MessageBox.Show("Ошибка сохранения файлы");
            }
            finally
            {
                System.Diagnostics.Process.Start(Path.GetTempPath() + "/1report.html");
            }
        }
    }
}