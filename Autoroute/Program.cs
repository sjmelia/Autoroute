using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Autoroute
{
    class Program
    {
        private const string IPHLPAPI = "iphlpapi.dll";

        [ComVisible(false), StructLayout(LayoutKind.Sequential)]
        internal struct MIB_IPFORWARDROW
        {
            internal uint /*DWORD*/ dwForwardDest;
            internal uint /*DWORD*/ dwForwardMask;
            internal int /*DWORD*/ dwForwardPolicy;
            internal uint /*DWORD*/ dwForwardNextHop;
            internal int /*DWORD*/ dwForwardIfIndex;
            internal int /*DWORD*/ dwForwardType;
            internal int /*DWORD*/ dwForwardProto;
            internal int /*DWORD*/ dwForwardAge;
            internal int /*DWORD*/ dwForwardNextHopAS;
            internal int /*DWORD*/ dwForwardMetric1;
            internal int /*DWORD*/ dwForwardMetric2;
            internal int /*DWORD*/ dwForwardMetric3;
            internal int /*DWORD*/ dwForwardMetric4;
            internal int /*DWORD*/ dwForwardMetric5;
        };

        enum MIB_IPFORWARD_TYPE
        {
            MIB_IPROUTE_TYPE_OTHER = 1,
            MIB_IPROUTE_TYPE_INVALID = 2,
            MIB_IPROUTE_TYPE_DIRECT = 3,
            MIB_IPROUTE_TYPE_INDIRECT = 4,
        }

        enum MIB_IPFORWARD_PROTOCOL
        {
            MIB_IPPROTO_OTHER = 1,
            MIB_IPPROTO_LOCAL = 2,
            MIB_IPPROTO_NETMGMT = 3,
            MIB_IPPROTO_ICMP = 4,
            MIB_IPPROTO_EGP = 5,
            MIB_IPPROTO_GGP = 6,
            MIB_IPPROTO_HELLO = 7,
            MIB_IPPROTO_RIP = 8,
            MIB_IPPROTO_IS_IS = 9,
            MIB_IPPROTO_ES_IS = 10,
            MIB_IPPROTO_CISCO = 11,
            MIB_IPPROTO_BBN = 12,
            MIB_IPPROTO_OSPF = 13,
            MIB_IPPROTO_BGP = 14,
            MIB_IPPROTO_NT_AUTOSTATIC = 10002,
            MIB_IPPROTO_NT_STATIC = 10006,
            MIB_IPPROTO_NT_STATIC_NON_DOD = 10007,
        };

        static void Main(string[] args)
        {

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            var adapter = adapters.Single(a => a.Description == "Intel(R) Wi-Fi 6E AX211 160MHz");

            var ipProperties = adapter.GetIPProperties();
            var ipv4Properties = ipProperties.GetIPv4Properties();
            var index = ipv4Properties.Index;

            bool running = true;
            var task = Task.Run(() =>
            {
                FixRoutes(index);
                while (running)
                {
                    NotifyRouteChange(default(uint), default(uint));
                    Console.WriteLine("Routes Changed");
                    FixRoutes(index);
                }
            });

            Console.ReadKey();
            running = false;
            task.GetAwaiter().GetResult();

        }

        private static void FixRoutes(int index)
        {
            //ping - n 100 localhost
            //    route delete 192.168.1.0
            //route add 192.168.1.0 MASK 255.255.255.0 0.0.0.0 IF 6 METRIC 1

            var route = new MIB_IPFORWARDROW
            {
                dwForwardDest = BitConverter.ToUInt32(IPAddress.Parse("192.168.0.0").GetAddressBytes(), 0),
                dwForwardMask = BitConverter.ToUInt32(IPAddress.Parse("255.255.0.0").GetAddressBytes(), 0),
                dwForwardNextHop = BitConverter.ToUInt32(IPAddress.Parse("0.0.0.0").GetAddressBytes(), 0),
                dwForwardMetric1 = 99,
                dwForwardType = (int)MIB_IPFORWARD_TYPE.MIB_IPROUTE_TYPE_INDIRECT,
                dwForwardProto = (int)MIB_IPFORWARD_PROTOCOL.MIB_IPPROTO_NETMGMT,
                dwForwardAge = 0,
                dwForwardIfIndex = index,
            };
            IntPtr pnt = Marshal.AllocHGlobal(Marshal.SizeOf(route));
            Marshal.StructureToPtr(route, pnt, false);

            var ipForwardEntry = CreateIpForwardEntry(pnt);
            Console.WriteLine(ipForwardEntry);
        }

        [DllImport(IPHLPAPI)]
        internal extern static uint NotifyRouteChange(uint nullhandle, uint nulloverlapped);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        private extern static int CreateIpForwardEntry(IntPtr /*PMIB_IPFORWARDROW*/ pRoute);


    }
}
