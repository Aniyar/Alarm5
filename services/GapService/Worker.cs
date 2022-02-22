using ALARm.Core;
using ALARm.Core.AdditionalParameteres;
using ALARm.Core.Report;
using ALARm.DataAccess;
using ALARm.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GapService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private string QueueName = "";
        public int tryCount = 0;
        public IMainTrackStructureRepository MainTrackStructureRepository;

        public Worker(ILogger<Worker> logger, IOptions<RabbitMQConfiguration> options)
        {
            _logger = logger;
            QueueName = options.Value.Queue;

            _connectionFactory = new ConnectionFactory
            {
                HostName = options.Value.Host,

                UserName = options.Value.Username,
                Password = options.Value.Password,
                VirtualHost = "/",
                Port = options.Value.Port,
            };
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Connection try [{tryCount++}].");
                _connection = _connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();
                //_channel.QueueDeclarePassive(QueueName);
                _channel.QueueDeclare(queue: QueueName,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);
                _channel.QueueBind(queue: QueueName,
                                   exchange: "alarm",
                                   routingKey: "");
                _channel.BasicQos(0, 1, false);
                _logger.LogInformation($"Queue [{QueueName}] is waiting for messages.");

                

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async(model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation(" [x] Received {0}", message);

                    JObject json = JObject.Parse(message);

                    var TripId = (int)json["TripId"];
                    var DistId = (int)json["DistId"];
                    var kmIndex = (int)json["KmIndex"];

                    //{
                    //    "TripId":240,
                    //    "DistId":45,
                    //    "KmIndex":728
                    //}



                    //var queruString = string.Join(",", fileId);

                    //if (queruString.Length == 0) 
                    //    return;

                    //var trip = RdStructureService.GetTripFromFileId((int)fileId.First()).Last();
                    //var kilometers = RdStructureService.GetKilometersByTrip(trip);
                    //var km = kilometers.Where(km => km.Number == kmIndex).ToList().First();
                    //this.MainTrackStructureRepository = MainTrackStructureService.GetRepository();

                    //var outData = (List<OutData>)RdStructureService.GetNextOutDatas(km.Start_Index - 1, km.GetLength() - 1, trip.Id);
                    //km.AddDataRange(outData, km);
                    //km.LoadTrackPasport(MainTrackStructureRepository, trip.Trip_date);

                    ////�������������
                    //// todo distanse id
                    //string p = GetGaps(trip, km, 53, queruString); //�����



                    var trips = RdStructureService.GetTrips();
                    var trip = trips.Where(trip => trip.Id == TripId).ToList().First();

                    var kilometers = RdStructureService.GetKilometersByTrip(trip);
                    var km = kilometers.Where(km => km.Number == kmIndex).ToList().First();
                    this.MainTrackStructureRepository = MainTrackStructureService.GetRepository();

                    var outData = (List<OutData>)RdStructureService.GetNextOutDatas(km.Start_Index - 1, km.GetLength() - 1, TripId);
                    km.AddDataRange(outData, km);

                    km.LoadTrackPasport(MainTrackStructureRepository, trip.Trip_date);
                    
                    try
                    {
                        GetGaps(trip, km, DistId); //�����
                        Console.WriteLine("���� ��!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("���� ERROR! " + e.Message);
                    }

                    //try
                    //{
                    //    GetBolt(trip, km, DistId); //�����
                    //    Console.WriteLine("���� ��!");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("���� ERROR! " + e.Message);
                    //}
                    //try
                    //{
                    //    GetBalast(trip, km, DistId); //������
                    //    Console.WriteLine("������ ��!");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("������ ERROR! " + e.Message);
                    //}

                    //try
                    //{
                    //    GetPerpen(trip, km, DistId);
                    //    Console.WriteLine("Perpen ��!");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("Perpen ERROR! " + e.Message);
                    //}

                    //try
                    //{
                    //    GetSleepers(trip, km, DistId);
                    //    Console.WriteLine("����� ��!");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("����� ERROR! " + e.Message);
                    //}

                    //try
                    //{
                    //    GetdeviationsinSleepers(trip, km, DistId); //��� �����
                    //    Console.WriteLine("��� ����� ��!");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("��� ����� ERROR! " + e.Message);
                    //}

                    //try
                    //{
                    //    Getbadfasteners(trip, km, DistId); //����������
                    //    Console.WriteLine("���������� ��!");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("���������� ERROR! " + e.Message);
                    //}
                    //try
                    //{
                    //    Getdeviationsinfastening(trip, km, DistId); //��� � �����
                    //    Console.WriteLine("��� ���� ���������� ��!");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine("��� ���� ���������� ERROR! " + e.Message);
                    //}
                };
                _channel.BasicConsume(queue: QueueName,
                                      autoAck: true,
                                      consumer: consumer);

                return base.StartAsync(cancellationToken);
            }
            catch(Exception e)
            {
                StartAsync(cancellationToken);
                return base.StartAsync(cancellationToken);
            }
        }




        /// <summary>
        /// ������ �� ������
        /// </summary>
        /// <param name="trip"></param>
        /// <param name="km"></param>
        /// <param name="distId"></param>
        private string GetBalast(Trips trip, Kilometer km, int distId, string query = "")
        {
            try
            {
                var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
                var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, distId) as AdmUnit;
                var trackName = AdmStructureService.GetTrackName(km.Track_id);
                var digressions = RdStructureService.GetBadRailFasteners(trip.Id, false, distance.Code, trackName);
                // if (badFasteners.Count == 0) continue;
                digressions = digressions.Where(o => o.Razn > 10 && o.Km > 128).ToList();
                var speed = new List<Speed>();
                RailFastener prev_fastener = null;
                foreach (var fastener in digressions)
                {
                    //string amount = (int)finddeg.Typ == 1025 ? finddeg.Length.ToString() + " ��.������" : finddeg.Length.ToString() + "%";
                    //string meter = (int)finddeg.Typ == 1025 ? (finddeg.Meter).ToString() : "";
                    //string piket = (int)finddeg.Typ != 1026 ? (finddeg.Meter / 100 + 1).ToString() : "";
                    var sector = "";
                    var previousKm = -1;
                    // if (fastener == null) continue;
                    //if (fastener.Razn < 300) continue;

                    if ((prev_fastener == null) || (prev_fastener.Km != fastener.Km))
                    {
                        sector = MainTrackStructureService.GetSector(km.Track_id, fastener.Km, trip.Trip_date);
                        sector = sector == null ? "" : sector;
                        speed = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, fastener.Km, MainTrackStructureConst.MtoSpeed, trip.Direction, trackName.ToString()) as List<Speed>;
                    }
                    fastener.PdbSection = km.PdbSection.Count > 0 ? $"���-{km.PdbSection[0].Pchu}/��-{km.PdbSection[0].Pd}/���-{km.PdbSection[0].Pdb}" : "���-/��-/���-";
                    fastener.Station = km.StationSection != null && km.StationSection.Count > 0 ?
                                      "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");
                    prev_fastener = fastener;

                    //   fastener.Fastening = GetName(fastener.Digressions[0].DigName);
                    //fastener.Station = sector;
                    //fastener.Fragment = sector;
                    fastener.Otst = fastener.Digressions[0].GetName();
                    fastener.Threat_id = fastener.Threat == Threat.Left ? "�����" : "������";
                }

                AdditionalParametersService.Insert_deviationsinballast(mainProcess.Trip_id, -1, digressions);
                return "Success";


           
            }
            catch (Exception e)
            {
                return "Error " + e.Message;
            }
        }
















        /// <summary>
        /// ������ �� ������
        /// </summary>
        /// <param name="trip"></param>
        /// <param name="km"></param>
        /// <param name="distId"></param>
        private string GetGaps(Trips trip, Kilometer km, int distId, string query = "")
        {
            try
            {
                var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
                var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, distId) as AdmUnit;
                var trackName = AdmStructureService.GetTrackName(km.Track_id);

                var gaps = AdditionalParametersService.GetFullGapsByNN(km.Number, trip.Id);


                var pass_speed = km.PdbSection.Count > 0 ? km.Speeds.First().Passenger : -1;
                var fr_speed = km.PdbSection.Count > 0 ? km.Speeds.First().Freight : -1;
                //var pdb = km.PdbSection.Count > 0 ? km.PdbSection[0].ToString() : " ��-/���-/��-/���-";
                //var sector = km.StationSection != null && km.StationSection.Count > 0 ?
                //                "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");
                var temperature = MainTrackStructureService.GetTemp(trip.Id, km.Track_id, km.Number);
                var temp = (temperature.Count != 0 ? temperature[0].Kupe.ToString() : " ") + "�";

                foreach (var gap in gaps)
                {
                    var pdb = km.PdbSection.Count > 0 ? km.PdbSection[0].ToString() : " ��-/���-/��-/���-";

                    var data = pdb.Split($"/").ToList();
                    if (data.Any())
                    {
                        pdb = $"{data[1]}/{data[2]}/{data[3]}";
                    }

                    var isStation = km.StationSection.Any() ?
                                    km.StationSection.Where(o => gap.Km + gap.Meter / 10000.0 >= o.RealStartCoordinate && o.RealFinalCoordinate >= gap.Km + gap.Meter / 10000.0).ToList() :
                                    new List<StationSection> { };

                    var sector1 = isStation.Any() ? "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");

                    gap.Pdb_section = pdb;
                    gap.Fragment = sector1;
                    gap.temp = temp;

                    gap.PassSpeed = pass_speed;
                    gap.FreightSpeed = fr_speed;

                    if (gap.Zazor != -1 || gap.Zazor != 1)

                        gap.Zazor = (int)(gap.Zazor * 0.8);

                    //gap.Zazor = (int)(gap.Zazor );

                    gap.GetDigressions436();
                    //var gap_l = gaps.Where(o => o.Threat == Threat.Left).ToList();
                    //var gap_r = gaps.Where(o => o.Threat == Threat.Right).ToList();

                    //var r = gap_r.Where(o => o.Km == gap.Km && (o.Meter >= gap.Meter - 1 && o.Meter <= gap.Meter + 1)).ToList();
                    //if (gap_l.Any())
                    //{
                    //    //if (gap.Zazor == -1)
                    //    //{
                    //        double k = (double)gap.H / (double)r.First().H;
                    //        gap.Zazor = (int)(r.First().Zazor * k);
                    //        gap.GetDigressions436();
                    //        if (gap.DigName.Name.Equals("�?"))
                    //            gap.DigName.Name = "�?�";
                    //        if (gap.DigName.Name.Equals("�"))
                    //            gap.DigName.Name = "��";
                    //   // }
                    //    //if (r.First().Zazor == -1)
                    //    //{
                    //    //    double k = (double)r.First().H / (double)gap.H;
                    //    //    r.First().Zazor = (int)(gap.Zazor * k);
                    //    //    r.First().GetDigressions436();
                    //    //    if (gap.DigName.Equals("�?"))
                    //    //        gap.DigName.Name = "�?�";
                    //    //    if (gap.DigName.Equals("�"))
                    //    //        gap.DigName.Name = "��";
                    //    //}

                    //}

                    //if (gap_r.Any())
                    //{


                    //    double k = (double)r.First().H / (double)gap.H;
                    //    r.First().Zazor = (int)(gap.Zazor * k);
                    //    r.First().GetDigressions436();
                    //    if (gap.DigName.Name.Equals("�?"))
                    //        gap.DigName.Name = "�?�";
                    //    if (gap.DigName.Name.Equals("�"))
                    //        gap.DigName.Name = "��";


                    //}

                }

                AdditionalParametersService.Insert_gap(mainProcess.Trip_id, -1, gaps);

                return "Success";
            }
            catch (Exception e)
            {
                return "Error " + e.Message;
            }
        }


        /// <summary>
        /// ������ ��������  ����������  
        /// </summary>
        /// <param name="trip"></param>
        /// <param name="km"></param>
        /// <param name="distId"></param>
        private void Getbadfasteners(Trips trip, Kilometer km, int distId)
        {
            var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, distId) as AdmUnit;
            var trackName = AdmStructureService.GetTrackName(km.Track_id);
            var badFasteners = RdStructureService.GetBadRailFasteners(trip.Id, false, distance.Code, trackName, km.Number);

            // if (badFasteners.Count == 0) continue;

            foreach (var fastener in badFasteners)
            {
                var pdb = km.PdbSection.Count > 0 ? km.PdbSection[0].ToString() : " ��-/���-/��-/���-";

                var data = pdb.Split($"/").ToList();
                if (data.Any())
                {
                    pdb = $"{data[1]}/{data[2]}/{data[3]}";
                }

                var sector1 = km.StationSection != null && km.StationSection.Count > 0 ?
                                "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");

                fastener.Pchu = pdb;
                fastener.Station = sector1;
                //fastener.Fastening =(string)GetName(fastener.Digressions[0].DigName);
                //fastener.Fastening = km.RailsBrace.Any() ? km.RailsBrace.First().Name : "��� ������";
                fastener.Fastening = fastener.ToString();

                fastener.Otst = fastener.Digressions[0].GetName();
                fastener.Threat_id = fastener.Threat == Threat.Left ? "�����" : "������";
            }
            AdditionalParametersService.Insert_badfastening(mainProcess.Trip_id, -1, badFasteners);

        }

        /// <summary>
        /// ������ ��� ���� �����
        /// </summary>
        /// <param name="trip"></param>
        /// <param name="km"></param>
        /// <param name="distId"></param>
        private void GetdeviationsinSleepers(Trips trip, Kilometer km, int distId)
        {
            var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
            var trackName = AdmStructureService.GetTrackName(km.Track_id);
            //var AbsSleepersList= RdStructureService.GetDigSleepers(mainProcess, MainTrackStructureConst.GetDigSleepers) as List<Digression>;
            var AbsSleepersList = RdStructureService.GetShpal(mainProcess, new int[] { 7 }, km.Number);

            AbsSleepersList = AbsSleepersList.OrderBy(o => o.Km).ThenBy(o => o.Meter).ToList();
            int countSl = 1;
            int prevM = -1;
            var digList = new List<Digression>();
            for (int i = 0; i <= AbsSleepersList.Count - 2; i++)
            {
                prevM = prevM == -1 ? AbsSleepersList[i].Km * 1000 + AbsSleepersList[i].Meter : prevM;
                var nextM = AbsSleepersList[i + 1].Km * 1000 + AbsSleepersList[i + 1].Meter;

                if (Math.Abs(prevM - nextM) < 2)
                {
                    prevM = nextM;
                    countSl++;
                }
                else if (countSl > 2)
                {
                    digList.Add(AbsSleepersList[i]);
                    digList[digList.Count - 1].Velich = countSl;
                    digList[digList.Count - 1].Ots = "���";

                    prevM = nextM;
                    countSl = 1;
                }
                else
                {
                    prevM = nextM;
                    countSl = 1;
                }
            }
            var previousKm = -1;
            var speed = new List<Speed>();
            var pdbSection = new List<PdbSection>();
            var sector = "";

            var rail_type = new List<RailsSections>();
            var skreplenie = new List<RailsBrace>();
            var shpala = new List<CrossTie>();
            var trackclasses = new List<TrackClass>();
            var curves = new List<StCurve>();

            List<Curve> curves1 = RdStructureService.GetCurvesInTrip(trip.Id) as List<Curve>;
            digList = digList.Where(o => o.Km == km.Number).ToList();

            foreach (var item in digList)
            {
                var KM = item.Km;

                //������ �� ��������� ��
                var curves2 = curves1.Where(
                    o => item.Km + item.Meter / 10000.0 >= o.RealStartCoordinate && o.RealFinalCoordinate >= item.Km + item.Meter / 10000.0).ToList();

                if ((previousKm == -1) || (previousKm != KM))
                {
                    //sector = MainTrackStructureService.GetSector(km.Track_id, km.Number, trip.Trip_date);
                    //sector = sector == null ? "" : sector;
                    //speed = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoSpeed, trip.Direction, trackName.ToString()) as List<Speed>;
                    //pdbSection = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoPdbSection, trip.Direction, trackName.ToString()) as List<PdbSection>;
                    rail_type = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoRailSection, trip.Direction, trackName.ToString()) as List<RailsSections>;
                    skreplenie = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoRailsBrace, trip.Direction, trackName.ToString()) as List<RailsBrace>;
                    trackclasses = (List<TrackClass>)MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoTrackClass, km.Track_id);
                    //curves = (List<StCurve>)MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoStCurve, km.Track_id);
                }
                previousKm = KM;

                var pdb = km.PdbSection.Count > 0 ? km.PdbSection[0].ToString() : " ��-/���-/��-/���-";

                var data = pdb.Split($"/").ToList();
                if (data.Any())
                {
                    pdb = $"{data[1]}/{data[2]}/{data[3]}";
                }

                var isStation = km.StationSection.Any() ?
                                km.StationSection.Where(o => item.Km + item.Meter / 10000.0 >= o.RealStartCoordinate && o.RealFinalCoordinate >= item.Km + item.Meter / 10000.0).ToList() :
                                new List<StationSection> { };

                var sector1 = isStation.Any() ? "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");

                if (item.Meter == 559)
                {
                    var rr = 0;
                }

                var curve = curves2.Any() ? curves2.First().Straightenings.Any() ? (int)curves2.First().Straightenings.First().Radius : -1 : -1;

                var ogr = "";

                switch (curve)
                {
                    case int cr when cr == -1 || cr >= 650:
                        if (rail_type[0].Name == "p65" || rail_type[0].Name == "p75")
                        {
                            switch (item.Velich)
                            {
                                case int c when c == 4:
                                    ogr = "60/40";
                                    break;
                                case int c when c == 5:
                                    ogr = "40/25";
                                    break;
                                case int c when c >= 6:
                                    ogr = "15/15";
                                    break;
                                default:
                                    ogr = "";
                                    break;
                            }
                        }
                        if (rail_type[0].Name == "p50")
                        {
                            switch (item.Velich)
                            {
                                case int c when c == 3:
                                    ogr = "50/40";
                                    break;
                                case int c when c == 4:
                                    ogr = "40/25";
                                    break;
                                case int c when c >= 5:
                                    ogr = "15/15";
                                    break;
                                default:
                                    ogr = "";
                                    break;
                            }
                        }
                        break;
                    default:
                        ogr = "";
                        break;
                }


                item.PCHU = pdb;
                item.Station = sector1;
                item.Speed = km.Speeds.Count > 0 ? km.Speeds.Last().ToString() : "-/-/-";

                item.Vpz = km.Speeds.Count > 0 ? km.Speeds[0].Passenger.ToString() + "/" + km.Speeds[0].Freight.ToString() : "-/-";
                item.Ots = item.Ots;
                item.TrackClass = (trackclasses.Count > 0 ? trackclasses[0].Class_Id : -1).ToString();
                item.Tripplan = curve != -1 ? "������ R-" + curve.ToString() : "������";

                //item.Fastening = skreplenie.Count > 0 ? skreplenie[0].Name : "��� ������";
                item.Fastening = km.RailsBrace.Any() ? km.RailsBrace.First().Name : "��� ������";
                item.Norma = km.Gauge.Count > item.Meter - 1 ? km.Gauge[item.Meter].ToString("0") : "��� ������";
                item.Kol = item.Velich + " ��";
                item.RailType = rail_type.Count > 0 ? rail_type[0].Name : "��� ������";
                item.Vdop = ogr;

            }

            AdditionalParametersService.Insert_sleepers(mainProcess.Trip_id, -1, digList);
        }

        /// <summary>
        /// ������ ��������� ������������� ������
        /// </summary>
        /// <param name="trip">������ �������</param>
        /// <param name="km">��������</param>
        /// <param name="DistId">�� id</param>
        public void GetBolt(Trips trip, Kilometer km, int DistId)
        {
            var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
            //����� �������
            var AbsBoltListLeft = RdStructureService.NoBolt(mainProcess, Threat.Left, km.Number);
            //������ �������
            var AbsBoltListRight = RdStructureService.NoBolt(mainProcess, Threat.Right, km.Number);
            List<Digression> AbsBoltList = new List<Digression>(AbsBoltListLeft);
            AbsBoltList.AddRange(AbsBoltListRight);
            AbsBoltList = AbsBoltList.OrderBy(o => o.Km).ThenBy(o => o.Meter).ToList();

            foreach (var item in AbsBoltList)
            {
                var pdb = km.PdbSection.Count > 0 ? km.PdbSection[0].ToString() : " ��-/���-/��-/���-";

                var data = pdb.Split($"/").ToList();
                if (data.Any())
                {
                    pdb = $"{data[1]}/{data[2]}/{data[3]}";
                }

                var isStation = km.StationSection.Any() ?
                                km.StationSection.Where(o => item.Km + item.Meter / 10000.0 >= o.RealStartCoordinate && o.RealFinalCoordinate >= item.Km + item.Meter / 10000.0).ToList() :
                                new List<StationSection> { };

                var sector1 = isStation.Any() ? "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");

                item.PCHU = pdb;
                item.Station = sector1;
                item.Speed = km.Speeds.Count > 0 ? km.Speeds.Last().Passenger + "/" + km.Speeds.Last().Freight : "-/-";
            }

            AdditionalParametersService.Insert_bolt(mainProcess.Trip_id, -1, AbsBoltList);
        }
        private void GetPerpen(Trips trip, Kilometer km, int distId)
        {
            var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
            var trackName = AdmStructureService.GetTrackName(km.Track_id);
            var skreplenie = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number,
                MainTrackStructureConst.MtoRailsBrace, trip.Direction, trackName.ToString()) as List<RailsBrace>;

            var ViolPerpen = RdStructureService.GetViolPerpen((int)trip.Id, new int[] { 7 }, km.Number);

            AdditionalParametersService.Insert_ViolPerpen(km, skreplenie, ViolPerpen);
        }
        /// <summary>
        /// ������ �� ��� ������
        /// </summary>
        /// <param name="trip"></param>
        /// <param name="km"></param>
        /// <param name="distId"></param>
        private void GetSleepers(Trips trip, Kilometer km, int distId)
        {
            try
            {
                var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
                var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, distId) as AdmUnit;
                var trackName = AdmStructureService.GetTrackName(km.Track_id);

                var digressions = RdStructureService.GetShpal(mainProcess, new int[] { 7 }, km.Number);


                List<Gap> check_gap_state = AdditionalParametersService.Check_gap_state(trip.Id, 0);

                var listIS = new List<int> { 10 };
                var listGAP = new List<int> { 7 };

                var previousKm = -1;
                var skreplenie = new List<RailsBrace>();
                var pdbSection = new List<PdbSection>();
                var sector = "";



                for (int i = 0; i < digressions.Count; i++)
                {
                    var isgap = false;

                    var c = check_gap_state.Where(o => o.Km + o.Meter / 10000.0 == digressions[i].Km + digressions[i].Meter / 10000.0).ToList();

                    if (c.Any())
                    {
                        isgap = true;
                    }
                    else
                    {
                        isgap = false;
                    }

                    if (digressions == null || digressions.Count == 0) continue;

                    if ((previousKm == -1) || (previousKm != digressions[i].Km))
                    {
                        //sector = MainTrackStructureService.GetSector(km.Track_id, digressions[i].Km, trip.Trip_date);
                        //sector = sector == null ? "��� ������" : sector;
                        //pdbSection = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, digressions[i].Km, MainTrackStructureConst.MtoPdbSection, trip.Direction, trackName.ToString()) as List<PdbSection>;
                        skreplenie = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, digressions[i].Km, MainTrackStructureConst.MtoRailsBrace, trip.Direction, trackName.ToString()) as List<RailsBrace>;
                    }

                    var pdb = km.PdbSection.Count > 0 ? km.PdbSection[0].ToString() : " ��-/���-/��-/���-";

                    var data = pdb.Split($"/").ToList(); 
                    if (data.Any())
                    {
                        pdb = $"{data[1]}/{data[2]}/{data[3]}";
                    }

                    var sector1 = km.StationSection != null && km.StationSection.Count > 0 ?
                                    "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");

                    previousKm = digressions[i].Km;

                    var otst = "";
                    var meropr = "";

                    switch (digressions[i].Oid)
                    {
                        case (int)VideoObjectType.Railbreak:
                            otst = "���������� �������";
                            meropr = "������ ��� ������� ������� ";
                            break;
                        case (int)VideoObjectType.Railbreak_Stone:
                            otst = "���������� �������";
                            meropr = "������ ��� ������� �������";
                            break;
                        case (int)VideoObjectType.Railbreak_vikol:
                            otst = "�����";
                            meropr = "������ ��� ������� ����������";
                            break;
                        case (int)VideoObjectType.Railbreak_raskol:
                            otst = "���������� ������";
                            meropr = "������ ��� ������� �������";
                            break;
                        case (int)VideoObjectType.Railbreak_midsection:
                            otst = "����� � ������� �����";
                            meropr = "�������������� ������ ��� ������� ����������";
                            break;
                        case (int)VideoObjectType.Railbreak_Stone_vikol:
                            otst = "�����";
                            meropr = "������ ��� ������� ����������";
                            break;
                        case (int)VideoObjectType.Railbreak_Stone_raskol:
                            if (i < digressions.Count - 2 && Math.Abs(digressions[i + 1].Meter - digressions[i].Meter) == 1)
                            {
                                otst = "���������� ������";
                                meropr = "�������������� ������ ��� ������� ����������";
                            }
                            else if (i > 0 && Math.Abs(digressions[i - 1].Meter - digressions[i].Meter) == 1)
                            {
                                otst = "���������� ������";
                                meropr = "�������������� ������ ��� ������� ����������";
                            }
                            else if (isgap)
                            {
                                otst = "���������� ������";
                                meropr = "�������������� ������ ��� ������� ����������";
                            }
                            else
                            {
                                otst = "���������� ������";
                                meropr = "������ ��� ������� �������";
                            }
                            break;
                        case (int)VideoObjectType.Railbreak_Stone_midsection:
                            if (i < digressions.Count - 2 && Math.Abs(digressions[i + 1].Meter - digressions[i].Meter) == 1)
                            {
                                otst = "����� � ������� �����";
                                meropr = "�������������� ������ ��� ������� ����������";
                            }
                            else if (i > 0 && Math.Abs(digressions[i - 1].Meter - digressions[i].Meter) == 1)
                            {
                                otst = "����� � ������� �����";
                                meropr = "�������������� ������ ��� ������� ����������";
                            }
                            else if (isgap)
                            {
                                otst = "����� � ������� �����";
                                meropr = "�������������� ������ ��� ������� ����������";
                            }
                            else
                            {
                                otst = "����� � ������� �����";
                                meropr = "������ ��� ������� �������";
                            }
                            break;
                    }

                    digressions[i].Otst = otst;
                    digressions[i].Meropr = meropr;
                    digressions[i].Pchu = pdb;
                    digressions[i].Station = sector1;
                    digressions[i].Fastening = skreplenie.Count > 0 ? skreplenie[0].Name : "��� ������";
                    digressions[i].Notice = isgap ? "����" : "";
                }

                AdditionalParametersService.Insert_defshpal(mainProcess.Trip_id, 1, digressions);

            }
            catch (Exception e)
            {
                Console.WriteLine("GetSleepers " + e.Message);
            }
        }
        /// <summary>
        /// ������ �� ������
        /// </summary>
        /// <param name="trip"></param>
        /// <param name="km"></param>
        /// <param name="distId"></param>

        private string Getdeviationsinfastening(Trips trip, Kilometer km, int distId)
        {
            try
            {
                var mainProcess = new MainParametersProcess { Trip_id = trip.Id };
                var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, distId) as AdmUnit;
                var trackName = AdmStructureService.GetTrackName(km.Track_id);

                //var getdeviationfastening = RdStructureService.GetBadRailFasteners(trip.Id, false, distance.Code, trackName);
                var getdeviationfastening = RdStructureService.GetBadRailFasteners(trip.Id, false, distance.Code, trackName, km.Number);
                // if (badFasteners.Count == 0) continue;

                RailFastener prev_fastener = null;
                var sector = "";
                int countSl = 1;
                int prevM = -1;
                int prevThreat = -1;
                var digList = new List<RailFastener>();

                for (int i = 0; i <= getdeviationfastening.Count - 2; i++)
                {
                    prevM = prevM == -1 ? getdeviationfastening[i].Km * 1000 + getdeviationfastening[i].Mtr : prevM;
                    prevThreat = prevThreat == -1 ? (int)getdeviationfastening[i].Threat : prevThreat;

                    var nextM = getdeviationfastening[i + 1].Km * 1000 + getdeviationfastening[i + 1].Mtr;
                    var nextThreat = (int)getdeviationfastening[i + 1].Threat;


                    if (Math.Abs(prevM - nextM) < 2)
                    {
                        if (prevThreat == nextThreat)
                        {
                            prevM = nextM;
                            countSl++;
                        }
                        else
                        {
                            if (countSl > 3)
                            {
                                digList.Add(getdeviationfastening[i]);
                                digList[digList.Count - 1].Count = countSl;
                                digList[digList.Count - 1].Ots = "���";

                                prevM = nextM;
                                countSl = 1;
                            }
                        }
                    }
                    else if (countSl > 3)
                    {
                        digList.Add(getdeviationfastening[i]);
                        digList[digList.Count - 1].Count = countSl;
                        digList[digList.Count - 1].Ots = "���";

                        prevM = nextM;
                        countSl = 1;

                    }
                    else
                    {
                        prevM = nextM;
                        countSl = 1;
                    }
                }

                RailFastener prev_digression = null;
                var speed = new List<Speed> { };
                var pdbSection = new List<PdbSection> { };
                var curves = new List<StCurve>();
                foreach (var digression in digList)
                {

                    if ((prev_digression == null) || (prev_digression.Km != digression.Km))
                    {
                        //tripplan = digression.Location == Location.OnCurveSection ? $"������ R-{digression.CurveRadius}" : (digression.Location == Location.OnStrightSection ? "������" : "���������� �������");
                        //amount = digression.DigName == DigressionName.KNS ? $"{digression.Count} ��" : $"{digression.Length} %";
                        speed = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoSpeed, trip.Direction, trackName.ToString()) as List<Speed>;
                        //pdbSection = MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoPdbSection, trip.Direction, trackName.ToString()) as List<PdbSection>;
                        //sector = MainTrackStructureService.GetSector(km.Track_id, km.Number, trip.Trip_date);
                        //sector = sector == null ? "" : sector;
                        curves = (List<StCurve>)MainTrackStructureService.GetMtoObjectsByCoord(trip.Trip_date, km.Number, MainTrackStructureConst.MtoStCurve, km.Track_id);
                    }
                    var curve = curves.Count > 0 ? (int)curves[0].Radius : -1;
                    var curveNorma = curves.Count > 0 ? (int)curves[0].Width : -1;

                    var ogr = "";

                    switch (curve)
                    {
                        case int cr when cr == -1 || cr >= 650:
                            switch (digression.Count)
                            {
                                case int c when c == 4:
                                    ogr = "60/60";
                                    break;
                                case int c when c == 5:
                                    ogr = "40/40    ";
                                    break;
                                case int c when c == 6:
                                    ogr = "25/25";
                                    break;
                                case int c when c > 6:
                                    ogr = "15/15";
                                    break;
                                default:
                                    ogr = "";
                                    break;
                            }
                            break;
                        case int cr when cr < 650:
                            switch (digression.Count)
                            {
                                case int c when c == 4:
                                    ogr = "40/40";
                                    break;
                                case int c when c == 5:
                                    ogr = "25/25";
                                    break;
                                case int c when c > 5:
                                    ogr = "15/15";
                                    break;
                                default:
                                    ogr = "";
                                    break;
                            }
                            break;
                        default:
                            ogr = "";
                            break;
                    }
                    var pdb = km.PdbSection.Count > 0 ? km.PdbSection[0].ToString() : " ��-/���-/��-/���-";

                    var data = pdb.Split($"/").ToList();
                    if (data.Any())
                    {
                        pdb = $"{data[1]}/{data[2]}/{data[3]}";
                    }

                    var sector1 = km.StationSection != null && km.StationSection.Count > 0 ?
                                    "�������: " + km.StationSection[0].Station : (km.Sector != null ? km.Sector.ToString() : "");
                    digression.Pchu = pdb;
                    //digression.Norma = ( curveNorma == -1 ? 1520 : curveNorma).ToString();
                    digression.Norma = km.Gauge.Count > digression.Mtr - 1 ? km.Gauge[digression.Mtr].ToString("0") : (curveNorma == -1 ? "��� ������" : curveNorma.ToString());

                    digression.Tripplan = curve != -1 ? "������ R-" + curve.ToString() : "������";
                    digression.Station = sector1;

                    prev_fastener = digression;

                    //digression.Fastening = (string)GetName(digression.Digressions[0].DigName);
                    digression.Fastening = km.RailsBrace.Any() ? km.RailsBrace.First().Name : "��� ������";
                    // fastener.Station = sector;
                    digression.Fragment = sector;
                    digression.Otst = digression.Digressions[0].GetName();
                    digression.Threat_id = digression.Threat == Threat.Left ? "�����" : "������";
                    digression.Velich = digression.Count + " ��";
                    digression.Vdop = ogr;
                    digression.Vpz = speed.Count > 0 ? speed[0].Passenger + "/" + speed[0].Freight : "";
                }
                AdditionalParametersService.Insert_deviationsinfastening(mainProcess.Trip_id, -1, digList);

                return "Success";
            }
            catch (Exception e)
            {
                return "Error " + e.Message;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // listen to the RabbitMQ messages, and send emails
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            _connection.Close();
            _logger.LogInformation("RabbitMQ connection is closed.");
        }
    }
 }
