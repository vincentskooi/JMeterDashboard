# JMeterDashboard
#Note: 06/05/2015 - Vincent Ooi.

Disclaimer: 
I was looking for a tool that could monitor the JMeter test in real-time. There are some JMeter listeners / plugins that create reports but often time they consumed too much memory which also impact the test itself. There are some sites that allow you to create pretty reports but only after the test run is completed where uploaded your jtl file. I have tried to look into that but found that their analysis of the result wasn't quite accurate and more importantly, it doesn't show me the real-time report. 

I couldn't find anything that fits my need. As a result, I created it myself. It is not the best thing on earth, but at least it allows you to monitor your test in real-time. You might not agree with the analysis that I have implemented, you have the full source code to implement what you think is right. However, I hope you can contribute back to this repository so everyone can use it for better good.

I don't do this full-time, I hope that someone out there could maybe enhance this tool and help to fix issues. 

This is WPF C#.net project, using WPFTool-kit Chart. There seems to have some limitation / performance issues if I want to load all data. Well, maybe I am not doing it the right way.
Anyway, hope it is useful for you. There are many things that I haven't implement.

######################################################################################
# JTL format:
# Here's an example of JTL file, the "threadName" should not have any space.
######################################################################################
timeStamp;elapsed;label;responseCode;threadName;bytes;grpThreads;allThreads;Latency;SampleCount;ErrorCount;Hostname
1433196603667;1364;Home Page;200;S02_EP_PatientList 2-1;10466;1;4;0;1;0;USSV-BC2ZNW1-D
1433196603528;1514;Home Page;200;S01_RecentTransmission 1-1;10466;1;4;0;1;0;USSV-BC2ZNW1-D
1433196603750;1348;Home Page;200;S03_TransmissionDetails_PatientList 3-1;10467;1;5;0;1;0;USSV-BC2ZNW1-D
1433196604315;942;Home Page;200;S03_TransmissionDetails_RecentTransmission 4-1;10467;1;6;0;1;0;USSV-BC2ZNW1-D
1433196605104;882;Home Page;200;S04_PatientProfile_RecentTransmission 6-1;10466;1;6;0;1;0;USSV-BC2ZNW1-D
1433196605044;1089;Home Page;200;S04_PatientProfile_PatientList 5-1;10495;1;6;0;1;0;USSV-BC2ZNW1-D
1433196609289;24094;Login to Recent Transmission;200;S04_PatientProfile_RecentTransmission 6-1;269958;1;6;0;1;0;USSV-BC2ZNW1-D
1433196608344;32291;Login to Recent Transmission;200;S01_RecentTransmission 1-1;56402;1;6;0;1;0;USSV-BC2ZNW1-D
1433196608561;35227;Login to Recent Transmission;200;S03_TransmissionDetails_RecentTransmission 4-1;76312;1;6;0;1;0;USSV-BC2ZNW1-D
1433196636811;7582;Navigate to Patient List;200;S04_PatientProfile_RecentTransmission 6-1;95645;1;6;0;1;0;USSV-BC2ZNW1-D
1433196647097;3397;Navigate Transmission Details;200;S03_TransmissionDetails_RecentTransmission 4-1;34760;1;6;0;1;0;USSV-BC2ZNW1-D
1433196647695;8321;Navigate to Patient Profile;200;S04_PatientProfile_RecentTransmission 6-1;24029;1;6;0;1;0;USSV-BC2ZNW1-D
1433196608400;80652;Login to Recent Transmission;200;S03_TransmissionDetails_PatientList 3-1;55379;1;6;0;1;0;USSV-BC2ZNW1-D
1433196694249;3479;Logout;200;S01_RecentTransmission 1-1;28467;1;6;0;1;0;USSV-BC2ZNW1-D

######################################################################################
#Example Screenshots:
#
######################################################################################

![Alt text](https://github.com/vincentskooi/JMeterDashboard/blob/master/Screenshots/Chart-RealTime.jpg "Real-Time Charts")

![Alt text](https://github.com/vincentskooi/JMeterDashboard/blob/master/Screenshots/SummaryPage.jpg "Overall Summary")

![Alt text](https://github.com/vincentskooi/JMeterDashboard/blob/master/Screenshots/Chart-Overall.jpg "Overall Charts")
