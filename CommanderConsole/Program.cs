using System.Threading;
using System;
using CMDSender.Gymbal.Ronin;

void PrintHelp()
{
    Console.WriteLine("사용 가능한 명령:");
    Console.WriteLine("  connect [ip] [port]  - 짐벌에 연결");
    Console.WriteLine("  move yaw roll pitch time(ms) - 지정 각도로 이동");
    Console.WriteLine("  speed yaw roll pitch - 축 속도 설정");
    Console.WriteLine("  getangle - 현재 각도 요청");
    Console.WriteLine("  recenter - 짐벌 리센터");
    Console.WriteLine("  version - 버전 정보 요청");
    Console.WriteLine("  exit - 프로그램 종료");
}

Console.WriteLine("DJI RS Command Console");
PrintHelp();

RoninComm? ronin = null;

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        continue;

    var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var cmd = parts[0].ToLowerInvariant();

    try
    {
        switch (cmd)
        {
            case "connect":
                if (parts.Length < 3)
                {
                    Console.WriteLine("사용법: connect [ip] [port]");
                    break;
                }
                ronin?.Dispose();
                ronin = new RoninComm();
                if (ronin.Connect(parts[1], ushort.Parse(parts[2])))
                {
                    Console.WriteLine("연결 성공");
                }
                else
                {
                    Console.WriteLine("연결 실패");
                }
                break;
            case "move":
                if (ronin == null)
                {
                    Console.WriteLine("먼저 connect 명령을 사용하세요.");
                    break;
                }
                if (parts.Length < 5)
                {
                    Console.WriteLine("사용법: move yaw roll pitch time(ms)");
                    break;
                }
                ronin.MoveTo(short.Parse(parts[1]), short.Parse(parts[2]), short.Parse(parts[3]), short.Parse(parts[4]));
                break;
            case "speed":
                if (ronin == null)
                {
                    Console.WriteLine("먼저 connect 명령을 사용하세요.");
                    break;
                }
                if (parts.Length < 4)
                {
                    Console.WriteLine("사용법: speed yaw roll pitch");
                    break;
                }
                ronin.SetSpeed(short.Parse(parts[1]), short.Parse(parts[2]), short.Parse(parts[3]));
                break;
            case "getangle":
                if (ronin == null)
                {
                    Console.WriteLine("먼저 connect 명령을 사용하세요.");
                    break;
                }
                ronin.GetAngle();
                Thread.Sleep(200);
                var att = ronin.Info.AttitudeAngle;
                Console.WriteLine($"Yaw={att.Yaw} Roll={att.Roll} Pitch={att.Pitch}");
                break;
            case "recenter":
                if (ronin == null)
                {
                    Console.WriteLine("먼저 connect 명령을 사용하세요.");
                    break;
                }
                ronin.Recenter();
                break;
            case "version":
                if (ronin == null)
                {
                    Console.WriteLine("먼저 connect 명령을 사용하세요.");
                    break;
                }
                ronin.GetVersion(true);
                Thread.Sleep(200);
                Console.WriteLine($"Version: {ronin.Info.VersionNumber}");
                break;
            case "help":
                PrintHelp();
                break;
            case "exit":
                ronin?.Dispose();
                return;
            default:
                Console.WriteLine("알 수 없는 명령입니다.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"오류: {ex.Message}");
    }
}
