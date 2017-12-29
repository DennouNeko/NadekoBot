using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Mabinogi.Services
{
    public class MabinogiDailiesService : INService
    {
        private readonly Logger _log;
        private readonly HttpClient _http;

        public DiscordSocketClient Client;
        public TimeSpan InitialInterval { get; private set; }

        public ConcurrentDictionary<ulong, IUserMessage> ChannelList { get; set; }
        public bool MessengerReady { get; private set; }

        private Dictionary<string, string> Translations;

        private Timer _t;

        public MabinogiDailiesService(NadekoBot bot, DiscordSocketClient client)
        {
            _log = LogManager.GetCurrentClassLogger();
            _http = new HttpClient();
            _http.AddFakeHeaders();
            Client = client;

            ChannelList = new ConcurrentDictionary<ulong, IUserMessage>();

            Translations = new Dictionary<string, string>();
            Translations.Add("타라", "Tara");
            Translations.Add("탈틴", "Taillteann");
            Translations.Add("(PC방)", "(VIP)");

            Translations.Add("도렌의 부탁", "Dorren's Request");
            Translations.Add("도발", "Provocation");
            Translations.Add("새도우 위자드 퇴치", "Defeat the Shadow Wizard");
            Translations.Add("정찰병 구출", "Rescue the Scout");
            Translations.Add("제물", "Offering");
            Translations.Add("탈틴 방어전", "Taillteann Defensive Battle");
            Translations.Add("탈틴 점령전 I", "Battle for Taillteann I");
            Translations.Add("탈틴 점령전 II", "Battle for Taillteann II");
            Translations.Add("포워르 커맨더 퇴치 I", "Defeat Fomor Commander I");
            Translations.Add("포워르 커맨더 퇴치 II", "Defeat Fomor Commander II");

            Translations.Add("그들의 방식", "Their Method");
            Translations.Add("그림자 세계의 유황거미", "The Sulfur Spider inside Shadow Realm");
            Translations.Add("그림자가 드리운 도시", "Shadow Cast City");
            Translations.Add("남아있는 어둠", "Lingering Darkness");
            Translations.Add("등 뒤의 적", "Enemy Behind");
            Translations.Add("또 다른 연금술사들", "The Other Alchemists");
            Translations.Add("파르홀론의 유령", "Ghost of Partholon");
            Translations.Add("포워르의 습격", "Fomor Attack");

            Run();
        }

        private void Run()
        {
            var now = DateTime.UtcNow;
            var dt = new DateTime(now.Year, now.Month, now.Day, now.Hour, 45, 0, DateTimeKind.Utc);
            if ((InitialInterval = dt.TimeOfDay - DateTime.UtcNow.TimeOfDay) < TimeSpan.Zero)
            {
                InitialInterval += TimeSpan.FromHours(1);
            }
            _log.Debug("Dailies initial trigger at " + dt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            _log.Debug("Triggering in " + InitialInterval.TotalMinutes + " minute(s)");

            _t = new Timer(async (_) =>
            {
                try { await Trigger().ConfigureAwait(false); } catch { }
            },
                null,
                InitialInterval,
                TimeSpan.FromHours(1)
            );
        }

        public void Reset()
        {
            Stop();
            Run();
        }

        public void Stop()
        {
            _t.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public string TryTranslate(string original)
        {
            if (Translations.ContainsKey(original))
                return Translations[original];
            return original;
        }

        public async Task Trigger()
        {
            _log.Debug("Dailies tick...");
            var PST = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
            var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, PST);

            if (now.Hour != 6) return;

            //var dayString = (DateTime.UtcNow-TimeSpan.FromDays(1)).ToString("yyyy-MM-dd");
            var dayString = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var dailyBaseUrl = "https://mabi-api.sigkill.kr/get_todayshadowmission/{0}?ndays=2";
            var dailyUrl = String.Format(dailyBaseUrl, dayString);
            var toSend = "🔄 I'm reminding you about dailies!";

            var data = "";
            _log.Info("Dailies for: " + dayString);
            _log.Debug("url: " + dailyUrl);
            try
            {
                data = await _http.GetStringAsync(dailyUrl).ConfigureAwait(false);
                _log.Debug("Dailies: " + data);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                return;
            }

            try
            {
                //toSend += "\nRAW reply:\n" + data;
                DailyItem[] parsed = JsonConvert.DeserializeObject<DailyItem[]>(data);

                toSend += "\n**Today**";
                toSend += "\nTaill: " + TryTranslate(parsed[0].Taillteann.normal.name);
                toSend += "\nTara: " + TryTranslate(parsed[0].Tara.normal.name);
                toSend += "\n**Tomorrow**";
                toSend += "\nTaill: " + TryTranslate(parsed[1].Taillteann.normal.name);
                toSend += "\nTara: " + TryTranslate(parsed[1].Tara.normal.name);
            }
            catch (Exception ex)
            {
                _log.Warn(ex);
                return;
            }

            foreach (var channelId in ChannelList.Keys)
            {
                if (!ChannelList.TryGetValue(channelId, out IUserMessage oldMsg)) continue;

                if(oldMsg != null)
                {
                    try
                    {
                        await oldMsg.DeleteAsync();
                    }
                    catch { }
                }

                try
                {
                    var channel = Client.GetChannel(channelId) as ITextChannel;

                    if (channel != null)
                    {
                        oldMsg = await channel.SendMessageAsync(toSend).ConfigureAwait(false);
                        ChannelList.AddOrUpdate(channelId, oldMsg, (key, old) =>
                        {
                            return oldMsg;
                        });
                    }
                }
                catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _log.Warn("Missing permissions. Could not send message to channel with ID : {0}", channelId);
                }
                catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
                {
                    _log.Warn("Channel not found : {0}", channelId);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            }
        }
    }

    public class DailyItem
    {
        public class Daily
        {
            [JsonProperty("name")]
            public string name { get; set; }
        }

        public class Town
        {
            [JsonProperty("pcbang")]
            public Daily pcbang { get; set; }

            [JsonProperty("normal")]
            public Daily normal { get; set; }
        }

        [JsonProperty("Taillteann")]
        public Town Taillteann { get; set; }

        [JsonProperty("Tara")]
        public Town Tara { get; set; }
    }
}
