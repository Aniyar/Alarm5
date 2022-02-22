﻿using ALARm.Core;
using ALARm.Core.Report;
using ALARm.Services;
using ALARm_Report.controls;
using Dapper;
using MetroFramework.Controls;
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
    public class AdditionalParameters_new : ALARm.Core.Report.GraphicDiagrams
    {
        private readonly float LevelPosition = 32.5f;
        private float longWaveLeftPosition;
        private float LongWaveRightPosition;
        private float MiddleWaveLeftPosition;
        private float MiddleWaveRightPosition;
        private float ShortWaveLeftPosition;
        private float ShortWaveRightPosition;
        private float ImpulsRoughnessLeftPosition;
        private float ImpulsRoughnessRightPosition;

        private readonly float GaugePosition = 100.5f;
        private float NPKLeftPosition = 57.5f + 23f;
        private float NPKRightPosition = 71.5f + 43f;

        private float PULeftPosition = 8.5f;
        private float PURightPosition = 41.5f;
        private float angleRuleWidth = 9.7f;

        public List<double> ShortWavesLeft { get; set; } = new List<double>();
        public List<double> ShortWavesRight { get; set; } = new List<double>();
        public List<double> MediumWavesLeft { get; set; } = new List<double>();
        public List<double> MediumWavesRight { get; set; } = new List<double>();
        public List<double> LongWavesLeft { get; set; } = new List<double>();
        public List<double> LongWavesRight { get; set; } = new List<double>();
        public List<Digression> Impuls { get; set; } = new List<Digression>();

        public List<int> Meters { get; set; } = new List<int>();

        private float GetDIstanceFrom1div60(float x)
        {
            return 15 * angleRuleWidth * (1f / x - 1 / 60f);
        }

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

            diagramName = "КН-1";
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
                var svgLength = 0;

                foreach (var trip in tripProcesses)
                {
                    foreach (var track_id in admTracksId)
                    {
                        longWaveLeftPosition = 5f;
                        LongWaveRightPosition = 23.8f;
                        MiddleWaveLeftPosition = 42.6f;
                        MiddleWaveRightPosition = 61.4f;
                        ShortWaveLeftPosition = 80.2f;
                        ShortWaveRightPosition = 99f;
                        ImpulsRoughnessLeftPosition = 122f;
                        ImpulsRoughnessRightPosition = 138f;

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
                        filters.Add(new FloatFilter() { Name = "Начало (км)", Value = lkm.Min() });
                        filters.Add(new FloatFilter() { Name = "Конец (км)", Value = lkm.Max() });

                        filterForm.SetDataSource(filters);
                        if (filterForm.ShowDialog() == DialogResult.Cancel)
                            return;

                        kilometers = kilometers.Where(Km => ((float)(float)filters[0].Value <= Km.Number && Km.Number <= (float)(float)filters[1].Value)).ToList();
                        kilometers = (trip.Travel_Direction == Direction.Reverse ? kilometers.OrderBy(o => o.Number) : kilometers.OrderByDescending(o => o.Number)).ToList();
                        if (kilometers.Count == 0) continue;
                        //--------------------------------------------

                        progressBar.Maximum = kilometers.Count;

                        foreach (var kilometer in kilometers)
                        {
                            kilometer.LoadTrackPasport(MainTrackStructureRepository, trip.Trip_date);
                            if (kilometer.NonstandardKms.Any())
                            {
                                kilometer.Final_m = kilometer.NonstandardKms.First().Len;
                            }

                            //данные
                            var DBcrossRailProfile = AdditionalParametersService.GetCrossRailProfileFromDBbyKm(kilometer.Number, trip.Id);
                            if (DBcrossRailProfile == null) continue;

                            var sortedData = DBcrossRailProfile.OrderByDescending(d => d.Meter).ToList();
                            var crossRailProfile = AdditionalParametersService.GetCrossRailProfileFromDBParse(sortedData);

                            XElement kmlist = new XElement("kmlist");

                            progressBar.Value = kilometers.IndexOf(kilometer) + 1;

                            var outData = (List<OutData>)RdStructureService.GetNextOutDatas(kilometer.Start_Index - 1, kilometer.GetLength() - 1, kilometer.Trip.Id);
                            kilometer.AddDataRange(outData, kilometer);

                            //lvl avg data
                            var Curves = new List<NatureCurves> { };
                            var StrightAvgTrapezoid = kilometer.StrightAvg.GetTrapezoid(new List<double> { }, new List<double> { }, 4, ref Curves);
                            var LevelAvgTrapezoid = kilometer.LevelAvg.GetTrapezoid(new List<double> { }, new List<double> { }, 10, ref Curves);
                            
                            LevelAvgTrapezoid.Add(LevelAvgTrapezoid[LevelAvgTrapezoid.Count - 1]);
                            StrightAvgTrapezoid.Add(StrightAvgTrapezoid[StrightAvgTrapezoid.Count - 1]);

                            //zero line data
                            kilometer.GetZeroLines(outData, trip, MainTrackStructureService.GetRepository());
                            kilometer.LoadDigresions(RdStructureRepository, MainTrackStructureRepository, trip, AdditionalParam:true);

                             var sector_station = MainTrackStructureService.GetSector(track_id, kilometer.Number, trip.Trip_date);
                            var fragment = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, kilometer.Number, MainTrackStructureConst.Fragments, kilometer.Direction_name, $"{trackName}") as Fragment;
                            var pdbSection = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, kilometer.Number, MainTrackStructureConst.MtoPdbSection, kilometer.Direction_name, $"{trackName}") as List<PdbSection>;

                            //Линий уровня по середине------------------------------------
                            List<string> result = new List<string>();
                            string drawdownRight = string.Empty, drawdownLeft = string.Empty, gauge = string.Empty, zeroGauge = string.Empty,
                                    zeroStraighteningRight = string.Empty, averageStraighteningRight = string.Empty, straighteningRight = string.Empty,
                                    zeroStraighteningLeft = string.Empty, averageStraighteningLeft = string.Empty, straighteningLeft = string.Empty,
                                    level = string.Empty, averageLevel = string.Empty, zeroLevel = string.Empty;

                            int fourStepOgrCoun = 0, otherfourStepOgrCoun = 0;


                            svgLength = kilometer.GetLength() < 1000 ? 1000 : kilometer.GetLength();
                            var xp = (-kilometer.Start_m - svgLength - 50) + (svgLength + 105) - 52;
                            var direction = AdmStructureRepository.GetDirectionByTrack(kilometer.Track_id);

                            XElement addParam = new XElement("addparam",
                                new XAttribute("top-title",
                                    (direction != null ? $"{direction.Name} ({direction.Code} )" : "Неизвестный") + " Путь: " + kilometer.Track_name + " Км:" +
                                    kilometer.Number + " " + (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].ToString() : " ПЧ-/ПЧУ-/ПД-/ПДБ-") + " Уст: " + " " +
                                            (kilometer.Speeds.Count > 0 ? $"{kilometer.Speeds.First().Passenger}/{kilometer.Speeds.First().Freight}" : "-/-") + $" Скор:{(int)kilometer.Speed.Average()}"),

                                new XAttribute("right-title",
                                    copyright + ": " + "ПО " + softVersion + "  " +
                                    systemName + ":" + trip.Car + "(" + trip.Chief.Trim() + ") (БПД от " + MainTrackStructureRepository.GetModificationDate() + ") <" + (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].RoadAbbr : "НЕИЗВ") + ">" + "<" + kilometer.Passage_time.ToString("dd.MM.yyyy  HH:mm") + ">" +
                                    "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Travel_Direction.ToString())) + ">" +
                                    "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Car_Position.ToString())) + ">" +
                                    "<" + trip.Trip_date.Month + "-" + trip.Trip_date.Year
                                     +
                                            " " + (trip.Trip_Type == TripType.Control ? "контр." : trip.Trip_Type == TripType.Work ? "раб." : "доп.") +
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
                                    new XElement("x", MMToPixelChartString(longWaveLeftPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(longWaveLeftPosition + 3.5f), new XAttribute("dasharray", "0.5,3"), new XAttribute("stroke", "grey")),
                                    new XElement("x", MMToPixelChartString(LongWaveRightPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(LongWaveRightPosition + 3.5f), new XAttribute("dasharray", "0.5,3"), new XAttribute("stroke", "grey")),
                                    new XElement("x", MMToPixelChartString(MiddleWaveLeftPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(MiddleWaveLeftPosition + 2.625f), new XAttribute("dasharray", "0.5,3"), new XAttribute("stroke", "grey")),
                                    new XElement("x", MMToPixelChartString(MiddleWaveRightPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(MiddleWaveRightPosition + 2.625f), new XAttribute("dasharray", "0.5,3"), new XAttribute("stroke", "grey")),
                                    new XElement("x", MMToPixelChartString(ShortWaveLeftPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(ShortWaveLeftPosition + 1.16666f), new XAttribute("dasharray", "0.5,3"), new XAttribute("stroke", "grey")),
                                    new XElement("x", MMToPixelChartString(ShortWaveRightPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(ShortWaveRightPosition + 1.16666f), new XAttribute("dasharray", "0.5,3"), new XAttribute("stroke", "grey")),
                                    new XElement("x", MMToPixelChartString(ImpulsRoughnessLeftPosition), new XAttribute("dasharray", "3,0"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(ImpulsRoughnessLeftPosition + 1.5f), new XAttribute("dasharray", "3,0"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(ImpulsRoughnessRightPosition), new XAttribute("dasharray", "3,0"), new XAttribute("stroke", "black")),
                                    new XElement("x", MMToPixelChartString(ImpulsRoughnessRightPosition + 1.5f), new XAttribute("dasharray", "3,0"), new XAttribute("stroke", "black"))
                                    ));


                            string Longwavesleft = string.Empty;
                            string Longwavesright = string.Empty;
                            string Mediumwavesleft = string.Empty;
                            string Mediumwavesright = string.Empty;
                            string Shortwavesleft = string.Empty;
                            string Shortwavesright = string.Empty;

                            float koefShort = 3.5f / 0.6f;
                            float koefMedium = 2.625f / 0.45f;
                            float koefLong = 1.1666f / 0.2f;
                            float koefIn = 0.7f;

                            var prevM = -1;
                            var correctM = 0.0;

                            GetTestData(kilometer.Number);


                            LongWavesLeft.Reverse();
                            LongWavesRight.Reverse();
                            MediumWavesLeft.Reverse();
                            MediumWavesRight.Reverse();
                            ShortWavesLeft.Reverse();
                            ShortWavesRight.Reverse();
                            for (int i = 0; i < kilometer.meter.Count - 1; i++)
                            {
                                try
                                {
                                    int metre = -kilometer.meter[i];

                                    if (LongWavesLeft.Count > i)
                                    {
                                        Longwavesleft += MMToPixelChartString(longWaveLeftPosition + koefLong * LongWavesLeft[i]).Replace(",", ".") + "," + metre + " ";
                                        Longwavesright += MMToPixelChartString(LongWaveRightPosition + koefLong * LongWavesRight[i]).Replace(",", ".") + "," + metre + " ";

                                        Mediumwavesleft += MMToPixelChartString(MiddleWaveLeftPosition + koefMedium * MediumWavesLeft[i]).Replace(",", ".") + "," + metre + " ";
                                        Mediumwavesright += MMToPixelChartString(MiddleWaveRightPosition + koefMedium * MediumWavesRight[i]).Replace(",", ".") + "," + metre + " ";

                                        Shortwavesleft += MMToPixelChartString(ShortWaveLeftPosition + koefShort * ShortWavesLeft[i]).Replace(",", ".") + "," + metre + " ";
                                        Shortwavesright += MMToPixelChartString(ShortWaveRightPosition + koefShort * ShortWavesRight[i]).Replace(",", ".") + "," + metre + " ";
                                    }
                                    else
                                    {
                                        Longwavesleft += MMToPixelChartString(longWaveLeftPosition + koefLong).Replace(",", ".") + "," + metre + " ";
                                        Longwavesright += MMToPixelChartString(LongWaveRightPosition + koefLong).Replace(",", ".") + "," + metre + " ";

                                        Mediumwavesleft += MMToPixelChartString(MiddleWaveLeftPosition + koefMedium).Replace(",", ".") + "," + metre + " ";
                                        Mediumwavesright += MMToPixelChartString(MiddleWaveRightPosition + koefMedium).Replace(",", ".") + "," + metre + " ";

                                        Shortwavesleft += MMToPixelChartString(ShortWaveLeftPosition + koefShort).Replace(",", ".") + "," + metre + " ";
                                        Shortwavesright += MMToPixelChartString(ShortWaveRightPosition + koefShort).Replace(",", ".") + "," + metre + " ";
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Рисование линий ошибка " + e.Message);
                                }
                            }

                            addParam.Add(new XElement("polyline", new XAttribute("points", Longwavesleft)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", Longwavesright)));

                            addParam.Add(new XElement("polyline", new XAttribute("points", Mediumwavesleft)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", Mediumwavesright)));

                            addParam.Add(new XElement("polyline", new XAttribute("points", Shortwavesleft)));
                            addParam.Add(new XElement("polyline", new XAttribute("points", Shortwavesright)));

                            char separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];

                            
                            var shortRoughness = new ShortRoughness { };

                            shortRoughness.ShortWaveRight.AddRange(ShortWavesRight.Select(o=> (float)o).ToList());
                            shortRoughness.MediumWaveRight.AddRange(MediumWavesRight.Select(o => (float)o).ToList());
                            shortRoughness.LongWaveRight.AddRange(LongWavesRight.Select(o => (float)o).ToList());

                            shortRoughness.ShortWaveLeft.AddRange(ShortWavesLeft.Select(o => (float)o).ToList());
                            shortRoughness.MediumWaveLeft.AddRange(MediumWavesLeft.Select(o => (float)o).ToList());
                            shortRoughness.LongWaveLeft.AddRange(LongWavesLeft.Select(o => (float)o).ToList());

                            shortRoughness.MetersLeft.AddRange(Meters);
                            shortRoughness.MetersRight.AddRange(Meters);

                            //импульсы
                            var impulses = Impuls;
                            for (int i = 0; i < impulses.Count; i++)
                            {
                                XElement impulsLeft = new XElement("impulsleft",
                                                        new XAttribute("x1", MMToPixelChartString(ImpulsRoughnessLeftPosition)),
                                                        new XAttribute("x3", MMToPixelChartString(ImpulsRoughnessLeftPosition + 1.5f))
                                                        );
                                XElement impulsRight = new XElement("impulsright",
                                                            new XAttribute("x1", MMToPixelChartString(ImpulsRoughnessRightPosition)),
                                                            new XAttribute("x3", MMToPixelChartString(ImpulsRoughnessRightPosition + 1.5f))
                                                            );

                                var impuls = impulses[i].Threat == Threat.Left ? impulsLeft : impulsRight;
                                var position = impulses[i].Threat == Threat.Left ? ImpulsRoughnessLeftPosition : ImpulsRoughnessRightPosition;
                                var digname = impulses[i].Threat == Threat.Left ? DigressionName.ImpulsLeft : DigressionName.ImpulsRight;
                                impulses[i].DigName = impulses[i].Threat == Threat.Left ? DigressionName.ImpulsLeft : DigressionName.ImpulsRight;

                                if ((impulses[i].Len > 0 && impulses[i].Len < 301) && impulses[i].Intensity_ra > 0.6f)
                                {
                                    float count = impulses[i].Len / 100.0f;
                                    float imp_count = impulses[i].Len / 40.0f;
                                    
                                    var Digressions = new List<DigressionMark>();

                                    Digressions.Add(new DigressionMark()
                                    {
                                        Digression = digname,
                                        NotMoveAlert = false,
                                        Meter = impulses[i].Meter,
                                        finish_meter = impulses[i].Meter + (int)count,
                                        Dlina = imp_count,

                                        Value = float.Parse(impulses[i].Intensity_ra.ToString("0.00")),
                                        Degree = 0,
                                        Count = (int)count,
                                        DigName = impulses[i].GetName(),
                                        PassengerSpeedLimit = -1,
                                        FreightSpeedLimit = -1,
                                        Comment = "",
                                        Diagram_type = "KN-1"
                                    });

                                    var picket = kilometer.Pickets.GetPicket(impulses[i].Meter);
                                    if (picket != null)
                                    {
                                        picket.Digression.Add(Digressions.First());
                                    }
                                }

                                impuls.Add(new XElement("imp",
                                    new XAttribute("x2", MMToPixelChartString(position - impulses[i].Length*0.5)),
                                    new XAttribute("x4", MMToPixelChartString(position + 1.5f + impulses[i].Intensity_ra )),
                                    new XAttribute("y", -impulses[i].Meter)
                                    ));
                                addParam.Add(impuls);
                            }

                            List<Digression> addDigressions = shortRoughness.GetDigressions();

                            foreach (var dig in addDigressions)
                            {
                                float count = dig.Length / 100.0f;

                                //if (count < 4) continue;
                                //if (dig.DigName != DigressionName.LongWaveLeft || dig.DigName != DigressionName.LongWaveRight ||
                                //    dig.DigName != DigressionName.MiddleWaveLeft || dig.DigName != DigressionName.MiddleWaveRight ||
                                //    dig.DigName != DigressionName.ShortWaveLeft || dig.DigName != DigressionName.ShortWaveRight
                                //    ) continue;

                                var Digressions = new List<DigressionMark>();

                                Digressions.Add(new DigressionMark()
                                {
                                    Digression = dig.DigName,
                                    NotMoveAlert = false,
                                    Meter = dig.Meter,
                                    finish_meter = dig.Meter + (int)count/5,
                                    Dlina = count/5,
                                    Value = float.Parse(dig.Value.ToString("0.00")),
                                    Degree = 0,
                                    Count = (int)count/5,
                                    DigName = dig.GetName(),
                                    PassengerSpeedLimit = -1,
                                    FreightSpeedLimit = -1,
                                    Comment = "",
                                    Diagram_type = "KN-1"
                                });

                                var picket = kilometer.Pickets.GetPicket(dig.Meter);
                                if (picket != null)
                                {
                                    picket.Digression.Add(Digressions.First());

                                }

                                picket.Digression = picket.Digression.OrderBy(o => o.Meter).ToList();
                            }

                          

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
                                    NPKRightPosition,
                                    NPKLeftPosition - 2,
                                    PURightPosition + 0.72f,
                                    PULeftPosition - 2.5f,
                                    GaugePosition,
                                    LevelPosition,
                                    this,
                                    ref fourStepOgrCoun,
                                    ref otherfourStepOgrCoun);
                            }
                           /// addParam.Add( new XAttribute("speedlimit", kilometer.GetdigressionsCount) );

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

        private void GetTestData(int number)
        {
            var connection = new Npgsql.NpgsqlConnection("Host=DESKTOP-EMAFC5J;Username=postgres;Password=alhafizu;Database=railway_copy");
            var ShortData = connection.Query<DataFlow>($@"SELECT * FROM testdata_242 where km = {number} limit 5000").ToList();

            var shortl = ShortData.Select(o => o.Diff_l / 8.0 < 1 / 8.0 ? 0 : o.Diff_l / 8.0).ToList();
            var shortr = ShortData.Select(o => o.Diff_r / 8.0 < 1 / 8.0 ? 0 : o.Diff_r / 8.0).ToList();

            Meters.AddRange(ShortData.Select(o => o.Meter).ToList());


            var val = new List<double> { };
            var val_ind = new List<int> { };
            var bolshe0 = new List<DATA0> { };
            var inn = false;

            //left
            for (int i = 0; i < shortl.Count - 1; i++)
            {
                var temp = shortl[i];
                var next_temp = shortl[i + 1];

                if (!inn && 0 < next_temp)
                {
                    val.Add(temp);
                    val_ind.Add(i);

                    val.Add(next_temp);
                    val_ind.Add(i + 1);

                    inn = true;
                }
                else if (inn && 0 < next_temp)
                {
                    val.Add(next_temp);
                    val_ind.Add(i + 1);

                }
                else if (inn && 0 == next_temp)
                {
                    if (val.Any())
                    {
                        val.Add(next_temp);
                        val_ind.Add(i + 1);

                        var d = new DATA0
                        {
                            Count = val.Count,
                            H = val.Max()*0.8,
                            H_ind = val_ind[val.IndexOf(val.Max())],
                        };

                        bolshe0.Add(d);

                        inn = false;
                        val.Clear();
                        val_ind.Clear();
                    }
                }
            }


            var val_r = new List<double> { };
            var val_ind_r = new List<int> { };
            var bolshe0_r = new List<DATA0> { };
            var inn_r = false;

            //right
            for (int i = 0; i < shortr.Count - 1; i++)
            {
                var temp = shortr[i];
                var next_temp = shortr[i + 1];

                if (!inn_r && 0 < next_temp)
                {
                    val_r.Add(temp);
                    val_ind_r.Add(i);

                    val_r.Add(next_temp);
                    val_ind_r.Add(i + 1);

                    inn_r = true;
                }
                else if (inn_r && 0 < next_temp)
                {
                    val_r.Add(next_temp);
                    val_ind_r.Add(i + 1);

                }
                else if (inn_r && 0 == next_temp)
                {
                    if (val_r.Any())
                    {
                        val_r.Add(next_temp);
                        val_ind_r.Add(i + 1);

                        var d = new DATA0
                        {
                            Count = val_r.Count,
                            H = val_r.Max()*0.8,
                            H_ind = val_ind_r[val_r.IndexOf(val_r.Max())],
                        };

                        bolshe0_r.Add(d);

                        inn_r = false;
                        val_r.Clear();
                        val_ind_r.Clear();
                    }
                }
            }

            for (int j = 0; j < shortl.Count; j++)
            {
                var m = 0.0;
                var l = 0.0;
                var s = 0.0;

                var mr = 0.0;
                var lr = 0.0;
                var sr = 0.0;

                for (int i = 0; i < bolshe0.Count; i++)
                {
                    l += bolshe0[i].H * Math.Exp(-0.003 * Math.Pow(bolshe0[i].H_ind - j, 2) / bolshe0[i].Count);
                    m += bolshe0[i].H * Math.Exp(-0.02 * Math.Pow(bolshe0[i].H_ind - j, 2) / bolshe0[i].Count);
                    s += bolshe0[i].H * Math.Exp(-0.3 * Math.Pow(bolshe0[i].H_ind - j, 2) / bolshe0[i].Count);
                }

                for (int i = 0; i < bolshe0_r.Count; i++)
                {
                    lr += bolshe0_r[i].H * Math.Exp(-0.003 * Math.Pow(bolshe0_r[i].H_ind - j, 2) / bolshe0_r[i].Count);
                    mr += bolshe0_r[i].H * Math.Exp(-0.02 * Math.Pow(bolshe0_r[i].H_ind - j, 2) / bolshe0_r[i].Count);
                    sr += bolshe0_r[i].H * Math.Exp(-0.3 * Math.Pow(bolshe0_r[i].H_ind - j, 2) / bolshe0_r[i].Count);


                }

                LongWavesLeft.Add(l * 0.1);
                MediumWavesLeft.Add(m * 0.1);
                ShortWavesLeft.Add(s * 0.1);

                LongWavesRight.Add(lr * 0.1);
                MediumWavesRight.Add(mr * 0.1);
                ShortWavesRight.Add(sr * 0.1);

            }
            //импульсы
            for (int i = 0; i < bolshe0.Count; i++)
            {
                if (bolshe0[i].H < 0.6) continue;

                Impuls.Add(new Digression
                {
                    Length = (int)bolshe0[i].Count,
                    Len = (int)bolshe0[i].Count,
                    Intensity_ra = bolshe0[i].H,
                    Meter = Meters[bolshe0[i].H_ind],
                    Threat = Threat.Left
                });
            }
            for (int i = 0; i < bolshe0_r.Count; i++)
            {
                if (bolshe0_r[i].H < 0.6) continue;

                Impuls.Add(new Digression
                {
                    Length = (int)bolshe0_r[i].Count,
                    Len = (int)bolshe0_r[i].Count,
                    Intensity_ra = bolshe0_r[i].H,
                    Meter = Meters[bolshe0_r[i].H_ind],
                    Threat = Threat.Right
                });
            }
        }
    

        private string GetXByDigName(DigName digName)
        {
            float move = 6.6f;
            switch (digName)
            {
                case DigName dn when dn == DigressionName.LongWaveLeft:
                    return MMToPixelChartString(LevelPosition + move);
                case DigName dn when dn == DigressionName.LongWaveRight:
                    return MMToPixelChartString(+move);
                case DigName dn when dn == DigressionName.MiddleWaveLeft:
                    return MMToPixelChartString(+move);
                case DigName dn when dn == DigressionName.MiddleWaveRight:
                    return MMToPixelChartString(+move);
                case DigName dn when dn == DigressionName.ShortWaveLeft:
                    return MMToPixelChartString(+move);
                case DigName dn when dn == DigressionName.ShortWaveRight:
                    return MMToPixelChartString(+move);
            }

            return "-100";
        }

        public class DATA0
        {
            public double H { get; set; }
            public int H_ind { get; set; }
            public double Count { get; set; }
            public double HR { get; internal set; }
        }

        public class DataFlow
        {
            public int Km { get; set; }
            public int Meter { get; set; }
            public double Diff_l { get; set; }
            public double Diff_r { get; set; }
        }
        
    }
}
