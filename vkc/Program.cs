using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.RequestParams;
using System.Media;
using System.IO;
using System.IO.Compression;

namespace vkc
{
    internal class Program
    {
        VkApi vk = new VkApi();
#region misc
        List<string> listFriends = new List<string>();
        Dictionary<long, string> cashe = new Dictionary<long, string>();
        static int soundcound = 0;
        static long tomessage;
        static int countNewMessage = 0;
        static int appKey = 5807051;
        static string vkPass;
        static string vkLogin;
        static long myId;
        static string casheDialogs = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Vkc";
#endregion

        private void VkIn()
        {
            try
            {
                
                vkPass = "";
                for (int i = 0; i < 10; i++) { Console.WriteLine(); }
                Console.Write("Введите логин (телефон, email): ");
                string vkLogin = Console.ReadLine();
                Console.Clear();
                for (int j = 0; j < 10; j++) {
                    Console.WriteLine();
                }
                Console.Write("Введите пароль: ");
                while (true) // скрывать вводимые символы
                {
                    ConsoleKeyInfo i = Console.ReadKey(true);
                    if (i.Key == ConsoleKey.Enter)
                    {
                        Console.Write('\n');
                        break;
                    }
                    else if (i.Key == ConsoleKey.Backspace)
                    {
                        if (vkPass.Length == 0) { continue;}
                        vkPass = vkPass.Remove(vkPass.Length - 1);
                        Console.Write("\b \b");
                    }
                    else
                    {
                        vkPass += i.KeyChar;
                        Console.Write("*");
                    }
                }
                Console.WriteLine();
                Settings settings = Settings.All;
                vk.Authorize(appKey, vkLogin, vkPass, settings);
                myId = long.Parse(vk.UserId.ToString());
                Console.Clear();
                for (int i = 0; i < 10; i++) { Console.WriteLine(); }
                Console.WriteLine("Авторизация успешна.");
                ThreadStart titleUpdate = new ThreadStart(Alerts); // запуск второго потока для обновления шапки
                Thread titleUnreadMessage = new Thread(titleUpdate);
                titleUnreadMessage.Start();
                ThreadStart dialogcashing = new ThreadStart(CashingDialogs); // запуск потока для кеширования пар id - имя-фамилия
                Thread cashingdialog = new Thread(dialogcashing);
                cashingdialog.Start();
                sw:
                Console.WriteLine("Сохранить этот аккаунт для быстрого входа? ( y или n ) ( Все Ваши данные шифруются )");
                string saveOrNot = Console.ReadLine();
                switch (saveOrNot)
                {
                    case "y":
                        Random rand = new Random();
                        string[] values = new string[] { vkLogin, vkPass };
                        string logins = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        File.WriteAllLines(logins + @"\" + rand.Next(1, 999999999) + ".vkltmp", values);

                        DirectoryInfo loginsTemp = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                        foreach (FileInfo loginfile in loginsTemp.GetFiles("*.vkltmp"))
                        {
                            var fB = File.ReadAllBytes(loginfile.FullName);
                            string encodedFile = Convert.ToBase64String(fB);
                            File.WriteAllText(logins + @"\" + rand.Next(1, 999999999) + ".vkltmp2", encodedFile);
                            foreach (FileInfo loginfile3 in loginsTemp.GetFiles("*.vkltmp"))
                            {
                                FileStream source = File.OpenRead(loginfile3.FullName);
                                FileStream distination = File.Create(logins + @"\" + rand.Next(1, 999999999) + ".vkl");

                                GZipStream compressor = new GZipStream(distination, CompressionMode.Compress);
                                int theByte = source.ReadByte();
                                while (theByte != -1)
                                {
                                    compressor.WriteByte((byte)theByte);
                                    theByte = source.ReadByte();
                                }
                                compressor.Close();
                                source.Close();
                                distination.Close();
                            }
                        }
                        foreach (FileInfo loginfile in loginsTemp.GetFiles("*.vkltmp"))
                        {
                            File.Delete(loginfile.FullName);
                        }
                        foreach (FileInfo loginfile in loginsTemp.GetFiles("*.vkltmp2"))
                        {
                            File.Delete(loginfile.FullName);
                        }
                        Console.WriteLine("Аккаунт успешно сохранен");
                        break;
                    case "n":
                        break;
                    default:
                        Console.WriteLine("Неправильный ввод.");
                        goto sw;
                }
                MainMenu();
            }
            catch (VkApiAuthorizationException error) {
                Console.WriteLine("Ошибка авторизации, попробуйте еще раз. Отладочная информация : " +error.HResult);
                VkIn();
            }

        } // авторизация 

        private void ChooseAccounts()
        {
            Dictionary<string, string> vkLogins = new Dictionary<string, string>();
            Dictionary<int, string> chooseAcc = new Dictionary<int, string>();
            DirectoryInfo logins = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            Directory.CreateDirectory(logins + @"\vkctemp"); 
            int prefixFilename = 0;
            foreach (FileInfo loginfile in logins.GetFiles("*.vkl"))
            {
                ++prefixFilename;
                DirectoryInfo loginsCrypt5 = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\vkctemp");
                try { loginfile.CopyTo(logins + @"\vkctemp\" + prefixFilename + ".vkl"); }
                catch
                {
                    foreach (FileInfo loginfile5 in loginsCrypt5.GetFiles("*"))
                    {
                        loginfile5.Delete();
                    }

                    Directory.Delete(logins + @"\vkctemp\");
                    ChooseAccounts();
                }
            }
            DirectoryInfo loginsCrypt = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\vkctemp");
            foreach (var cryptoFile in loginsCrypt.GetFiles("*.vkl")) // расшифровываем
            {
                FileStream source = File.OpenRead(cryptoFile.FullName);
                FileStream distination = File.Create(cryptoFile.FullName + ".tmp"); 
                GZipStream compressor = new GZipStream(source, CompressionMode.Decompress);
                byte[] compr = File.ReadAllBytes(cryptoFile.FullName); 
                byte[] decode = Decompress(compr);
                foreach (var cryptoFil5e in loginsCrypt.GetFiles("*.vkl")) // расшифровываем
                {
                    Console.WriteLine("Расшифровка сохраненных аккаунтов...");
                    source.Close();
                    distination.Close(); Thread.Sleep(700); // необходима задержка чтобы виделось >1 .vkl файла
                }
                Random rand = new Random(); 
                File.WriteAllBytes(logins + @"\vkctemp\" + rand.Next(1, 999999999) + ".vkld", decode); 
            }
            foreach (FileInfo loginfile in loginsCrypt.GetFiles("*.vkld"))
            {
                string vkLogin = File.ReadAllLines(loginfile.FullName).First();
                string vkPass = File.ReadAllLines(loginfile.FullName).Skip(1).First();
                try { vkLogins.Add(vkLogin, vkPass); }
                catch { Console.WriteLine("Аккаунт с таким логином уже сохранен"); }
            }
            Console.WriteLine("Доступные логины :");
            int loginCount = 0;
            
            foreach(var login in vkLogins.Keys)
            {
                ++loginCount;
                Console.WriteLine(loginCount + " " + login);
                chooseAcc.Add(loginCount , login);
            }
            Console.Write("Выберите логин ( 0 - чтобы войти под другой учетной записью) : ");
            int choosedLogin = int.Parse(Console.ReadLine());
            if (choosedLogin == 0) { VkIn(); }
            vkLogin = chooseAcc[choosedLogin];
            vkPass = vkLogins[vkLogin];
            Settings settings = Settings.All;
            try
            {
                vk.Authorize(appKey, vkLogin, vkPass, settings);
            }
            catch { Console.WriteLine("Не удалось подключиться. Возможно, вы сменили пароль. Вы можете сбросить сохраненные аккаунты в настройках");
                Console.ReadKey(true); Environment.Exit(1);
            }
            myId = long.Parse(vk.UserId.ToString());
            Console.Clear();
            for (int i = 0; i < 10; i++) { Console.WriteLine(); }
            Console.WriteLine("Авторизация успешна.");
            ThreadStart dialogcashing = new ThreadStart(CashingDialogs); // запуск потока для кеширования пар id - имя-фамилия
            Thread cashingdialog = new Thread(dialogcashing);
            cashingdialog.Start();
            foreach (FileInfo loginfile in loginsCrypt.GetFiles("*"))
            {
                loginfile.Delete();
            }

                Directory.Delete(logins + @"\vkctemp\");
            ThreadStart titleUpdate = new ThreadStart(Alerts); // запуск второго потока для обновления шапки
            Thread titleUnreadMessage = new Thread(titleUpdate);
            titleUnreadMessage.Start();
            MainMenu();
        } // выбор сохраненных аккаунтов

        private static void Main()
        {
            Console.Title = "VK cmd";            
            Program prog = new Program();
            int foundLogins = 0;
            DirectoryInfo logins = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            foreach (FileInfo loginfile in logins.GetFiles("*.vkl"))
            { ++foundLogins; }
            if (foundLogins>0)
            { prog.ChooseAccounts();}
            prog.VkIn();
        } // точка входа

        private void MainMenu()
        {
            Console.WriteLine(@"Доступны команды: (/f)riends, (/m)essages, (/r)eply, (/s)end, (/c)lear , (/set)tings, /help, /quit");
            Console.Write("Введите команду: ");
            string cmd = Console.ReadLine();
            Console.WriteLine();
            switch (cmd)
            {
                case "/set":
                case "/settings":
                    {
                        VKCSettings();
                        break;
                    }

                case "/m":
                case "/messages":
                    {
                        Messages();
                        MainMenu();
                        break;
                    }

                case "/friends":
                case "/f":
                    {
                        ChooseFriend();
                        break;
                    }

                case "/send":
                case "/s":
                {
                    SendMessage();
                    break;
                }
                case "/quit":
                {
                    Environment.Exit(0);
                    break;
                }

                case "/r":
                case "/reply":
                    Reply();
                    break;

                case "/c":
                case "/clear":
                    Console.Clear();
                    for (int i = 0; i < 10; i++) { Console.WriteLine(); }
                    MainMenu();
                    break;
                case "/help":
                    Help();
                    break;

                default:
                {
                    Console.WriteLine("Я не знаю такой команды");
                    MainMenu();
                    break;
                }
            }
        } // общее меню

        private void GetFriends()
        {
            var users = vk.Friends.Get(myId, ProfileFields.FirstName | ProfileFields.LastName);
            foreach (var friends in users)
            {
                listFriends.Add(friends.FirstName + " " + friends.LastName + " " + friends.LastSeen + " " + friends.Id + " " +friends.Online);
            }
            
            foreach (string line in listFriends)
            {
                string line2 = line.Replace("True","*Online*");
                string line3 = line2.Replace("False", "");
                Console.WriteLine(line3);
            }
            MainMenu();
        } // получение списка друзей

        private void SendMessage()
        {
            try
            {
                Console.Write("Введите Id кому ( 0 - чтобы выйти ): ");
                try
                {
                    long id = long.Parse(Console.ReadLine());
                    if (id == 0) { MainMenu(); }
                    Console.WriteLine();
                    Console.WriteLine("Введите Сообщение:");
                    string message = Console.ReadLine();
                    var send = vk.Messages.Send(new MessagesSendParams { UserId = id, Message = message });
                    Console.WriteLine("Успешно отправленно.");
                    MainMenu();
                }
                catch(FormatException)
                {
                    Console.WriteLine("Неверно введен Id, попробуйте снова.");
                    SendMessage();
                }
            }
            catch (VkApiException e)
            {
                Console.WriteLine("Ошибка! Сообщение не отправлено.");
                Console.WriteLine(e);
                MainMenu();
            }
        } // разовая отправка сообщений

        private void Alerts()
        {
            while (true)
            {
                
                var getDialogs = vk.Messages.GetDialogs(new MessagesDialogsGetParams
                {
                    Count = 200,
                    Unread = true
                });
                if (getDialogs.Messages.Count > 0)
                {
                    if (soundcound == 0)
                    {
                        if (!File.Exists(casheDialogs + @"\sound.off"))
                        {
                            soundcound = 1;
                            SoundPlayer sp = new SoundPlayer(@"c:\Windows\Media\Windows Notify.wav");
                            sp.Play();
                            Thread.Sleep(1000);
                            sp.Dispose();
                        }
                    }
                    foreach (var line in getDialogs.Messages)
                    {
                        countNewMessage += line.Unread;
                        Console.Title = "VK cmd Диалогов с новыми сообщениями : " + getDialogs.Messages.Count.ToString() + " ***"+IdToName(long.Parse(line.UserId.ToString()))+ line.Body + "   ***";
                        tomessage = long.Parse(line.UserId.ToString());
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    Console.Title = "VK cmd   Новых сообщений нет";
                    soundcound = 0;

                }
                Thread.Sleep(3000);
            }
        } // показ в шапке окна новых сообщений + уведомления

        private string IdToName(long id)
        {
            
                if (cashe.ContainsKey(id)) // сессионное кеширование пар id - имя фамилия
                {
                    return cashe[id];
                }
                if(File.Exists(casheDialogs+@"\"+id.ToString()))
                {
                return File.ReadAllText(casheDialogs + @"\" + id.ToString());
                }
            if (cashe.ContainsKey(id)) // сессионное кеширование пар id - имя фамилия
            {
                return cashe[id];
            }
            else
            {
                try
                {
                    cashe.Add(id, vk.Users.Get(id).FirstName + " " + vk.Users.Get(id).LastName + " : "); Thread.Sleep(550); // Задержка чтобы вк не ругался
                    var user = vk.Users.Get(id);
                    return user.FirstName + " " + user.LastName + " : ";
                }
                catch {
                    var user = vk.Users.Get(id);
                    return user.FirstName + " " + user.LastName + " : ";
                }
            }
            
        } // конвертер айди пользака в имя фамилию с кешированием

        private void CreateDialog(long userid)
        {

            while (true)
            {
                Console.Clear();
                var history = vk.Messages.GetHistory(new MessagesGetHistoryParams
                {
                    Count = 15,
                    UserId = userid

                });
                var reverseMessage = history.Messages.Reverse();
                foreach (var message in reverseMessage)
                {
                    long ids = (long)message.FromId;
                    Console.WriteLine(message.Date.ToString()+" "+ IdToName(ids) + message.Body);
                }
                Thread historyclean = new Thread(delegate () { HistoryCleaner(history.Messages); }); // создание потока на пометку сообщений прочитанными
                historyclean.Start();
                Console.WriteLine();
                Console.Write(@"Введите сообщение ( или (/r)eply, (/e)xit, (/re)efresh): ");
                string inputmes = Console.ReadLine();
                switch (inputmes)
                {
                    case "/r":
                    case "/reply":
                        Reply();
                        break;

                    case "/exit":
                    case "/e":
                    {
                        MainMenu();
                        break;
                    }

                    case "/re":
                    case "/refresh":
                    {
                        CreateDialog(userid);
                            break;
                    }
                   
                    default:
                    {
                            if (inputmes == "" || inputmes == null)
                            {
                                Console.WriteLine("Нельзя отправить пустое сообщение!");
                            }
                            else
                            {
                                vk.Messages.Send(new MessagesSendParams { UserId = userid, Message = inputmes });
                                CreateDialog(userid);
                            }
                        break;

                    }
                }
            }

        } // создает диалог с пользователем, показывает историю, прочитывает сообщения

        private void Messages()
        {
            if (!File.Exists(casheDialogs + @"\done.check")) { Console.WriteLine("Подождите, кэширование диалогов еще не завершенно."); Console.WriteLine("Эта единократная операция занимает около 3-5 минут и значительно ускоряет работу всей программы."); MainMenu(); }


            
            var dialogs = vk.Messages.GetDialogs(new MessagesDialogsGetParams
            {
                Count = 200,
                Unread = false
            });
            int dialogCount = int.Parse(dialogs.TotalCount.ToString())+1;

            var dialogReverse = dialogs.Messages.Reverse();

            Dictionary<int , long> myDialogs = new Dictionary<int, long>();
            foreach (var dialog in dialogReverse)
            {
                --dialogCount;
                Console.WriteLine(dialogCount+ " " + IdToName(long.Parse(dialog.UserId.ToString())) + " " + dialog.Body);
                myDialogs.Add(dialogCount, long.Parse(dialog.UserId.ToString()));
            }
            Console.WriteLine("Выберите диалог или 0 чтобы выйти :");
            string choosedDialog = Console.ReadLine();
            try
            {
                switch (choosedDialog)
                {
                    case "0":
                        MainMenu();
                        break;
                    default:
                        CreateDialog(myDialogs[int.Parse(choosedDialog)]);
                        break;
                }
            }
            catch { Console.WriteLine("Неверный ввод. Введите цифру, обозначающую порядковый номер диалога"); Messages(); }
        } // выбор контакта по диалогам(а не по друзьям )

        private void Reply()
        {
            var newQuickDialog1 = vk.Messages.GetDialogs(new MessagesDialogsGetParams
            {
                Count = 200,
                Unread = true,
            });

            if (newQuickDialog1.TotalCount != 0)
            {
                soundcound = 1;
                Dictionary<int, long> newDialogs = new Dictionary<int, long>();
                int countNewDialogs = 0;
                var newQuickDialog = vk.Messages.GetDialogs(new MessagesDialogsGetParams
                {
                    Count = 200,
                    Unread = true,
                });
                foreach (var nqd in newQuickDialog.Messages)
                {
                    ++countNewDialogs;
                    newDialogs.Add(countNewDialogs, long.Parse(nqd.UserId.ToString()));
                    Console.WriteLine(countNewDialogs + " " + IdToName(long.Parse(nqd.UserId.ToString())) + nqd.Body.ToString());
                }
                Console.Write("Выберите кому ответить (0 чтобы выйти) : ");
                try
                {
                    int ch = int.Parse(Console.ReadLine());
                    if (ch == 0) { MainMenu(); }
                    else if (ch > countNewDialogs) { Console.WriteLine("Нет диалога с таким номером"); Reply(); }
                    else { CreateDialog(newDialogs[ch]); }
                }
                catch
                {
                    Console.WriteLine("Требуется ввести номер диалога (цифру) чтобы ответить");
                    Console.WriteLine("Нажмите любую клавишу, чтобы продолжить..."); Console.ReadKey(true);
                }
            }
            else { Console.WriteLine("Нет новых сообщений"); MainMenu(); }
        } // быстрый ответ на новый диалог ( требуется выбрать )

        private void HistoryCleaner(System.Collections.ObjectModel.ReadOnlyCollection<Message> history)
        {
            foreach (var line in history)
            {
                vk.Messages.MarkAsRead(long.Parse(line.Id.ToString()));
                Thread.Sleep(500); // нельзя слишком много запросов
            }
        } // делает прочитанными переданную коллекцию сообщений

        private void Help()
        {
            Console.Clear();
            Console.WriteLine("Из разных подменю программы доступны разные команды.");
            Console.WriteLine("Главное меню: ");
            Console.WriteLine(@"/friends или /f -  Создает диалог (чат) с другом, показывает историю, делает сообщения прочитанными при показе. Требуется выбрать друга");
            Console.WriteLine(@"/messages или /m  - Создает диалог (чат) с пользователем из списка активных диалогов. Требуется выбрать диалог.");
            Console.WriteLine(@"/send или /s - Быстрая посылка сообщения, требуется id.");
            Console.WriteLine(@"/clear или /c - Очищает окно, после чего показывает главное меню");
            Console.WriteLine(@"/reply или /r - Ответить на сообщение из нового диалога. Невозможен если нет новых диалогов.");
            Console.WriteLine(@"/settings или /set - Вызывает меню настроек.");
            Console.WriteLine(@"/help  - Показывает эту страницу - Помощь.");
            Console.WriteLine(@"/quit - Безапелляционный выход из приложения.");
            Console.WriteLine();
            Console.WriteLine("Меню диалога: ");
            Console.WriteLine("В режиме диалога любой текст кроме указанных команд - отправка этого текста собеседнику.");
            Console.WriteLine(@"/reply или /r - Ответить на сообщение из нового диалога. Невозможен если нет новых диалогов.");
            Console.WriteLine(@"/exit или /e - Выход из диалога в главное меню.");
            Console.WriteLine(@"/refresh или /re - Обновить сообщения текущего диалога (новые сообщения автоматически показываются только в заголовке консольного окна.");
            Console.WriteLine();
            Console.WriteLine("Меню настроек : ");
            Console.WriteLine(@"/clearacc - Удаляет все сохраненные аккаунты.");
            Console.WriteLine(@"/soundoff - Выключает звук уведомлений о новых сообщениях");
            Console.WriteLine(@"/soundon - Включает звук уведомлений о новых сообщениях ( beta , звук работает только в случае если не было новых сообщений");
            Console.WriteLine(@"/exit или /e - Выход в главное меню.");
            Console.WriteLine("Нажмите любую кнопку, чтобы продолжить...");
            Console.ReadKey(true);
            Console.Clear();
            MainMenu();
        } // вызывает помощь

        private void ChooseFriend()
        {
            Dictionary<int, long> allFriends = new Dictionary<int, long>();
            var chooseFriend = vk.Friends.Get(myId, ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Uid | ProfileFields.Online);
            int friendCount = 0;
            foreach(var friend in chooseFriend)
            {
                ++friendCount;
                allFriends.Add(friendCount, friend.Id);
                string on = BoolToString(bool.Parse(friend.Online.ToString()));
                Console.WriteLine(friendCount.ToString()+" " + friend.FirstName + " "+ friend.LastName + " " + on);
            }
            Console.WriteLine("Выберите номер контакта с которым хотите продолжить диалог ( 0 чтобы выйти, -1 написать себе ): ");
            int chs = int.Parse(Console.ReadLine());
            if (chs == 0) { MainMenu(); }
            if (chs == -1) { CreateDialog(myId); }
            try { long choosed = allFriends[chs]; CreateDialog(choosed); }
            catch
            {
                Console.WriteLine("Требуется ввести именно номер контакта (цифра)");
                Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
                Console.ReadKey(true);
                ChooseFriend();
            }
            
        } // быстрый выбор контакта для диалога

        private string BoolToString(bool online)
        {
            if (online == true)
            {
                return "***Online***";
            }
            else { return ""; }
        } // преабразует bool в string ( контакт онлайн или нет ) 

        private byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);

                        }

                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        } // расжимает файл аккаунта

        private void VKCSettings()
        {
            Console.WriteLine("Доступны команды : /clearacc, /soundoff, /soundon (/e)xit");
            string choosedSettings = Console.ReadLine();
            switch (choosedSettings)
            {
                case "/soundon":
                    if (File.Exists(casheDialogs + @"\sound.off"))
                    {
                        File.Delete(casheDialogs + @"\sound.off");
                    }
                    Console.WriteLine("Звук включен");
                    break;
                        

                case "/soundoff":
                    if(!File.Exists(casheDialogs+@"\sound.off"))
                    {
                        File.Create(casheDialogs + @"\sound.off");
                    }
                    Console.WriteLine("Звук выключен");
                    VKCSettings();
                    break;

                case "/clearacc": // удаляет все сохраненные логины пароли
                    {
                        DeleteAccs();
                        MainMenu();
                        break;
                    }
                case "/e":
                case "/exit":
                    {
                        MainMenu();
                        break;
                    }

                default:
                    {
                        Console.WriteLine("Я не знаю такой команды");
                        VKCSettings();
                        break;
                    }
            }
            
        } // тут будут некоторые настройки

        private void DeleteAccs()
        {
            DirectoryInfo logins = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            foreach (FileInfo loginfile in logins.GetFiles("*.vkl"))
            {
                loginfile.Delete();
            }
            Console.WriteLine("Больше файлов нет.");
        } // удаляет сохраненные аккаунты

        private void CashingDialogs() // кеширует пары id - имя-фио для списка диалогов
        {
            if (!Directory.Exists(casheDialogs)) { Directory.CreateDirectory(casheDialogs);}
            
            var dialogs = vk.Messages.GetDialogs(new MessagesDialogsGetParams
            {
                Count = 200,
                Unread = false
            });
            foreach (var dialog in dialogs.Messages)
            {
                long tempId = long.Parse(dialog.UserId.ToString());
                if (tempId < 1) { continue; }
                if (cashe.ContainsKey(long.Parse(dialog.UserId.ToString()))) { continue; }
                if(File.Exists(casheDialogs+@"\"+ dialog.UserId.ToString())) { continue; }
                File.WriteAllText(casheDialogs +@"\"+ dialog.UserId, IdToName(long.Parse(dialog.UserId.ToString())));
            }
            File.Create(casheDialogs + @"\done.check"); // файл говорящий что не нужно производить полное перекеширование
        }
    }
}
