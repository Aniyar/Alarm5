using ALARm.Core;
using Microsoft.AspNetCore.Components;
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


namespace MainService
{
    public class Worker : BackgroundService
    {
       
        
        private readonly ILogger<Worker> _logger;
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private string QueueName = "";
        private IRdStructureRepository RdStructureRepository;
        public int tryCount = 0;
        //{'FileId':17105, 'Km':42, 'Path': '\SCL-12\common\video_objects\desktop\213_17105_km_42.csv'}
        public Worker(ILogger<Worker> logger, IRdStructureRepository rdStructureRepository, IOptions<RabbitMQConfiguration> options)
        {
            QueueName = options.Value.Queue;
            _logger = logger;
            RdStructureRepository = rdStructureRepository;
            
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
                _channel.QueueDeclarePassive(QueueName);
                _channel.BasicQos(0, 1, false);
                _logger.LogInformation($"Queue [{QueueName}] is waiting for messages.");
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body).Replace("'","\"").Replace("\\", "\\\\");
                    var file = JObject.Parse(message).ToObject<File>();
                    file.Path = file.Path.Replace(@"\DESKTOP-EMAFC5J", @"G:");
                    var res = RdStructureRepository.RunRvoDataInsert(file.Km, file.FileId, file.Path);
                    if (res != null) {
                        if ((Group)res[0] == Group.VideoObjects && res.Count() > 4)
                        {
                            var msg = "{'FileId':["+ $"{res[1]},{res[2]},{res[3]},{res[4]}" + "], 'KmIndex':" + file.Km + "}";
                            Helper.SendMessageToRabbitMQExchange(_connection, "alarm",  msg);
                        }
                    }
                    _logger.LogInformation(res != null ? res.ToString() : "");
                    
                };
                _channel.BasicConsume(queue: QueueName,
                                     autoAck: true,
                                     consumer: consumer);

                return base.StartAsync(cancellationToken);
            }
            catch
            {
                StartAsync(cancellationToken);
                return base.StartAsync(cancellationToken);
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
