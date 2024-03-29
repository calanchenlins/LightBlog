﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AspectCore.Extensions.Reflection;
using DotNext.Reflection;
using BenchmarkDotNet.Attributes;

namespace KaneBlake.Basis.Performance.Benchmarks
{
    public class StringSearchBenchmark
    {
        private readonly string commandText = @"SET NOCOUNT ON;
UPDATE [sw_SCPG_c] SET [line_name] = @p0, [loc_id] = @p1, [loc_name] = @p2
WHERE [com_id] = @p3 AND [vou_no] = @p4 AND [vou_no_line] = @p5;
SELECT @@ROWCOUNT;

UPDATE [sw_SCPG_c] SET [line_name] = @p6, [loc_id] = @p7, [loc_name] = @p8
WHERE [com_id] = @p9 AND [vou_no] = @p10 AND [vou_no_line] = @p11;
SELECT @@ROWCOUNT;

UPDATE [sw_SCPG_c] SET [line_name] = @p12, [loc_id] = @p13, [loc_name] = @p14
WHERE [com_id] = @p15 AND [vou_no] = @p16 AND [vou_no_line] = @p17;
SELECT @@ROWCOUNT;

UPDATE [sw_SCPG_c] SET [done_qty] = @p18, [line_name] = @p19, [loc_id] = @p20, [loc_name] = @p21
WHERE [com_id] = @p22 AND [vou_no] = @p23 AND [vou_no_line] = @p24;
SELECT @@ROWCOUNT;

UPDATE [sw_SCPG_c] SET [line_name] = @p25, [loc_id] = @p26, [loc_name] = @p27
WHERE [com_id] = @p28 AND [vou_no] = @p29 AND [vou_no_line] = @p30;
SELECT @@ROWCOUNT;

UPDATE [sw_SCPG_c] SET [line_name] = @p31, [loc_id] = @p32, [loc_name] = @p33
WHERE [com_id] = @p34 AND [vou_no] = @p35 AND [vou_no_line] = @p36;
SELECT @@ROWCOUNT;

DECLARE @inserted6 TABLE ([com_id] char(4), [vou_no] char(14), [vou_no_line] int, [_Position] [int]);
MERGE [sw_SCPG_cc] USING (
VALUES (@p37, @p38, @p39, @p40, @p41, @p42, @p43, @p44, @p45, @p46, @p47, @p48, @p49, @p50, @p51, @p52, @p53, @p54, @p55, @p56, @p57, @p58, @p59, @p60, @p61, @p62, @p63, @p64, @p65, @p66, @p67, @p68, @p69, @p70, @p71, @p72, @p73, @p74, 0),
(@p75, @p76, @p77, @p78, @p79, @p80, @p81, @p82, @p83, @p84, @p85, @p86, @p87, @p88, @p89, @p90, @p91, @p92, @p93, @p94, @p95, @p96, @p97, @p98, @p99, @p100, @p101, @p102, @p103, @p104, @p105, @p106, @p107, @p108, @p109, @p110, @p111, @p112, 1),
(@p113, @p114, @p115, @p116, @p117, @p118, @p119, @p120, @p121, @p122, @p123, @p124, @p125, @p126, @p127, @p128, @p129, @p130, @p131, @p132, @p133, @p134, @p135, @p136, @p137, @p138, @p139, @p140, @p141, @p142, @p143, @p144, @p145, @p146, @p147, @p148, @p149, @p150, 2),
(@p151, @p152, @p153, @p154, @p155, @p156, @p157, @p158, @p159, @p160, @p161, @p162, @p163, @p164, @p165, @p166, @p167, @p168, @p169, @p170, @p171, @p172, @p173, @p174, @p175, @p176, @p177, @p178, @p179, @p180, @p181, @p182, @p183, @p184, @p185, @p186, @p187, @p188, 3),
(@p189, @p190, @p191, @p192, @p193, @p194, @p195, @p196, @p197, @p198, @p199, @p200, @p201, @p202, @p203, @p204, @p205, @p206, @p207, @p208, @p209, @p210, @p211, @p212, @p213, @p214, @p215, @p216, @p217, @p218, @p219, @p220, @p221, @p222, @p223, @p224, @p225, @p226, 4),
(@p227, @p228, @p229, @p230, @p231, @p232, @p233, @p234, @p235, @p236, @p237, @p238, @p239, @p240, @p241, @p242, @p243, @p244, @p245, @p246, @p247, @p248, @p249, @p250, @p251, @p252, @p253, @p254, @p255, @p256, @p257, @p258, @p259, @p260, @p261, @p262, @p263, @p264, 5),
(@p265, @p266, @p267, @p268, @p269, @p270, @p271, @p272, @p273, @p274, @p275, @p276, @p277, @p278, @p279, @p280, @p281, @p282, @p283, @p284, @p285, @p286, @p287, @p288, @p289, @p290, @p291, @p292, @p293, @p294, @p295, @p296, @p297, @p298, @p299, @p300, @p301, @p302, 6),
(@p303, @p304, @p305, @p306, @p307, @p308, @p309, @p310, @p311, @p312, @p313, @p314, @p315, @p316, @p317, @p318, @p319, @p320, @p321, @p322, @p323, @p324, @p325, @p326, @p327, @p328, @p329, @p330, @p331, @p332, @p333, @p334, @p335, @p336, @p337, @p338, @p339, @p340, 7),
(@p341, @p342, @p343, @p344, @p345, @p346, @p347, @p348, @p349, @p350, @p351, @p352, @p353, @p354, @p355, @p356, @p357, @p358, @p359, @p360, @p361, @p362, @p363, @p364, @p365, @p366, @p367, @p368, @p369, @p370, @p371, @p372, @p373, @p374, @p375, @p376, @p377, @p378, 8),
(@p379, @p380, @p381, @p382, @p383, @p384, @p385, @p386, @p387, @p388, @p389, @p390, @p391, @p392, @p393, @p394, @p395, @p396, @p397, @p398, @p399, @p400, @p401, @p402, @p403, @p404, @p405, @p406, @p407, @p408, @p409, @p410, @p411, @p412, @p413, @p414, @p415, @p416, 9),
(@p417, @p418, @p419, @p420, @p421, @p422, @p423, @p424, @p425, @p426, @p427, @p428, @p429, @p430, @p431, @p432, @p433, @p434, @p435, @p436, @p437, @p438, @p439, @p440, @p441, @p442, @p443, @p444, @p445, @p446, @p447, @p448, @p449, @p450, @p451, @p452, @p453, @p454, 10),
(@p455, @p456, @p457, @p458, @p459, @p460, @p461, @p462, @p463, @p464, @p465, @p466, @p467, @p468, @p469, @p470, @p471, @p472, @p473, @p474, @p475, @p476, @p477, @p478, @p479, @p480, @p481, @p482, @p483, @p484, @p485, @p486, @p487, @p488, @p489, @p490, @p491, @p492, 11),
(@p493, @p494, @p495, @p496, @p497, @p498, @p499, @p500, @p501, @p502, @p503, @p504, @p505, @p506, @p507, @p508, @p509, @p510, @p511, @p512, @p513, @p514, @p515, @p516, @p517, @p518, @p519, @p520, @p521, @p522, @p523, @p524, @p525, @p526, @p527, @p528, @p529, @p530, 12),
(@p531, @p532, @p533, @p534, @p535, @p536, @p537, @p538, @p539, @p540, @p541, @p542, @p543, @p544, @p545, @p546, @p547, @p548, @p549, @p550, @p551, @p552, @p553, @p554, @p555, @p556, @p557, @p558, @p559, @p560, @p561, @p562, @p563, @p564, @p565, @p566, @p567, @p568, 13),
(@p569, @p570, @p571, @p572, @p573, @p574, @p575, @p576, @p577, @p578, @p579, @p580, @p581, @p582, @p583, @p584, @p585, @p586, @p587, @p588, @p589, @p590, @p591, @p592, @p593, @p594, @p595, @p596, @p597, @p598, @p599, @p600, @p601, @p602, @p603, @p604, @p605, @p606, 14),
(@p607, @p608, @p609, @p610, @p611, @p612, @p613, @p614, @p615, @p616, @p617, @p618, @p619, @p620, @p621, @p622, @p623, @p624, @p625, @p626, @p627, @p628, @p629, @p630, @p631, @p632, @p633, @p634, @p635, @p636, @p637, @p638, @p639, @p640, @p641, @p642, @p643, @p644, 15),
(@p645, @p646, @p647, @p648, @p649, @p650, @p651, @p652, @p653, @p654, @p655, @p656, @p657, @p658, @p659, @p660, @p661, @p662, @p663, @p664, @p665, @p666, @p667, @p668, @p669, @p670, @p671, @p672, @p673, @p674, @p675, @p676, @p677, @p678, @p679, @p680, @p681, @p682, 16),
(@p683, @p684, @p685, @p686, @p687, @p688, @p689, @p690, @p691, @p692, @p693, @p694, @p695, @p696, @p697, @p698, @p699, @p700, @p701, @p702, @p703, @p704, @p705, @p706, @p707, @p708, @p709, @p710, @p711, @p712, @p713, @p714, @p715, @p716, @p717, @p718, @p719, @p720, 17),
(@p721, @p722, @p723, @p724, @p725, @p726, @p727, @p728, @p729, @p730, @p731, @p732, @p733, @p734, @p735, @p736, @p737, @p738, @p739, @p740, @p741, @p742, @p743, @p744, @p745, @p746, @p747, @p748, @p749, @p750, @p751, @p752, @p753, @p754, @p755, @p756, @p757, @p758, 18),
(@p759, @p760, @p761, @p762, @p763, @p764, @p765, @p766, @p767, @p768, @p769, @p770, @p771, @p772, @p773, @p774, @p775, @p776, @p777, @p778, @p779, @p780, @p781, @p782, @p783, @p784, @p785, @p786, @p787, @p788, @p789, @p790, @p791, @p792, @p793, @p794, @p795, @p796, 19),
(@p797, @p798, @p799, @p800, @p801, @p802, @p803, @p804, @p805, @p806, @p807, @p808, @p809, @p810, @p811, @p812, @p813, @p814, @p815, @p816, @p817, @p818, @p819, @p820, @p821, @p822, @p823, @p824, @p825, @p826, @p827, @p828, @p829, @p830, @p831, @p832, @p833, @p834, 20),
(@p835, @p836, @p837, @p838, @p839, @p840, @p841, @p842, @p843, @p844, @p845, @p846, @p847, @p848, @p849, @p850, @p851, @p852, @p853, @p854, @p855, @p856, @p857, @p858, @p859, @p860, @p861, @p862, @p863, @p864, @p865, @p866, @p867, @p868, @p869, @p870, @p871, @p872, 21),
(@p873, @p874, @p875, @p876, @p877, @p878, @p879, @p880, @p881, @p882, @p883, @p884, @p885, @p886, @p887, @p888, @p889, @p890, @p891, @p892, @p893, @p894, @p895, @p896, @p897, @p898, @p899, @p900, @p901, @p902, @p903, @p904, @p905, @p906, @p907, @p908, @p909, @p910, 22)) AS i ([com_id], [vou_no], [vou_no_line], [Split_qty], [bf_confirm_fill_date], [bf_confirm_usr_id], [bf_confirm_usr_name], [confirm_done_qty], [confirm_lf_qty], [confirm_zf_qty], [done_fixed_time], [done_flag], [done_qty], [estimate_flag], [fixed_time], [group_flag], [input_bc], [input_date], [lf_code], [lf_qty], [lf_txt], [line_id], [line_name], [loc_id], [loc_name], [m_vou_no_line], [notes], [process_name], [process_no], [sys_id], [sys_process_no], [unit_confirm_fill_date], [unit_st_fill_date], [usr_id], [usr_name], [zf_code], [zf_qty], [zf_txt], _Position) ON 1=0
WHEN NOT MATCHED THEN
INSERT ([com_id], [vou_no], [vou_no_line], [Split_qty], [bf_confirm_fill_date], [bf_confirm_usr_id], [bf_confirm_usr_name], [confirm_done_qty], [confirm_lf_qty], [confirm_zf_qty], [done_fixed_time], [done_flag], [done_qty], [estimate_flag], [fixed_time], [group_flag], [input_bc], [input_date], [lf_code], [lf_qty], [lf_txt], [line_id], [line_name], [loc_id], [loc_name], [m_vou_no_line], [notes], [process_name], [process_no], [sys_id], [sys_process_no], [unit_confirm_fill_date], [unit_st_fill_date], [usr_id], [usr_name], [zf_code], [zf_qty], [zf_txt])
VALUES (i.[com_id], i.[vou_no], i.[vou_no_line], i.[Split_qty], i.[bf_confirm_fill_date], i.[bf_confirm_usr_id], i.[bf_confirm_usr_name], i.[confirm_done_qty], i.[confirm_lf_qty], i.[confirm_zf_qty], i.[done_fixed_time], i.[done_flag], i.[done_qty], i.[estimate_flag], i.[fixed_time], i.[group_flag], i.[input_bc], i.[input_date], i.[lf_code], i.[lf_qty], i.[lf_txt], i.[line_id], i.[line_name], i.[loc_id], i.[loc_name], i.[m_vou_no_line], i.[notes], i.[process_name], i.[process_no], i.[sys_id], i.[sys_process_no], i.[unit_confirm_fill_date], i.[unit_st_fill_date], i.[usr_id], i.[usr_name], i.[zf_code], i.[zf_qty], i.[zf_txt])
OUTPUT INSERTED.[com_id], INSERTED.[vou_no], INSERTED.[vou_no_line], i._Position
INTO @inserted6;

SELECT [t].[fiscal_period], [t].[fiscal_year] FROM [sw_SCPG_cc] t
INNER JOIN @inserted6 i ON ([t].[com_id] = [i].[com_id]) AND ([t].[vou_no] = [i].[vou_no]) AND ([t].[vou_no_line] = [i].[vou_no_line])
ORDER BY [i].[_Position];

INSERT INTO [sw_SCPG_cc_vou_lot] ([Bsub_vou], [com_id], [sub_com_id], [sub_djid], [sub_mdid], [vou_no], [item_stock_no], [lot_no], [order_qty], [qr_flag], [st_flag], [sub_dept_id], [sub_item_name], [sub_item_no], [sub_item_pattern], [sub_item_spec], [sub_item_stock_no], [sub_line_id], [sub_loc_id], [sub_lot_no], [sub_order_qty], [sub_process_no], [sub_unit_id], [sub_unit_name], [sub_vou_no])
VALUES (@p911, @p912, @p913, @p914, @p915, @p916, @p917, @p918, @p919, @p920, @p921, @p922, @p923, @p924, @p925, @p926, @p927, @p928, @p929, @p930, @p931, @p932, @p933, @p934, @p935),
(@p936, @p937, @p938, @p939, @p940, @p941, @p942, @p943, @p944, @p945, @p946, @p947, @p948, @p949, @p950, @p951, @p952, @p953, @p954, @p955, @p956, @p957, @p958, @p959, @p960),
(@p961, @p962, @p963, @p964, @p965, @p966, @p967, @p968, @p969, @p970, @p971, @p972, @p973, @p974, @p975, @p976, @p977, @p978, @p979, @p980, @p981, @p982, @p983, @p984, @p985),
(@p986, @p987, @p988, @p989, @p990, @p991, @p992, @p993, @p994, @p995, @p996, @p997, @p998, @p999, @p1000, @p1001, @p1002, @p1003, @p1004, @p1005, @p1006, @p1007, @p1008, @p1009, @p1010),
(@p1011, @p1012, @p1013, @p1014, @p1015, @p1016, @p1017, @p1018, @p1019, @p1020, @p1021, @p1022, @p1023, @p1024, @p1025, @p1026, @p1027, @p1028, @p1029, @p1030, @p1031, @p1032, @p1033, @p1034, @p1035),
(@p1036, @p1037, @p1038, @p1039, @p1040, @p1041, @p1042, @p1043, @p1044, @p1045, @p1046, @p1047, @p1048, @p1049, @p1050, @p1051, @p1052, @p1053, @p1054, @p1055, @p1056, @p1057, @p1058, @p1059, @p1060),
(@p1061, @p1062, @p1063, @p1064, @p1065, @p1066, @p1067, @p1068, @p1069, @p1070, @p1071, @p1072, @p1073, @p1074, @p1075, @p1076, @p1077, @p1078, @p1079, @p1080, @p1081, @p1082, @p1083, @p1084, @p1085),
(@p1086, @p1087, @p1088, @p1089, @p1090, @p1091, @p1092, @p1093, @p1094, @p1095, @p1096, @p1097, @p1098, @p1099, @p1100, @p1101, @p1102, @p1103, @p1104, @p1105, @p1106, @p1107, @p1108, @p1109, @p1110),
(@p1111, @p1112, @p1113, @p1114, @p1115, @p1116, @p1117, @p1118, @p1119, @p1120, @p1121, @p1122, @p1123, @p1124, @p1125, @p1126, @p1127, @p1128, @p1129, @p1130, @p1131, @p1132, @p1133, @p1134, @p1135),
(@p1136, @p1137, @p1138, @p1139, @p1140, @p1141, @p1142, @p1143, @p1144, @p1145, @p1146, @p1147, @p1148, @p1149, @p1150, @p1151, @p1152, @p1153, @p1154, @p1155, @p1156, @p1157, @p1158, @p1159, @p1160),
(@p1161, @p1162, @p1163, @p1164, @p1165, @p1166, @p1167, @p1168, @p1169, @p1170, @p1171, @p1172, @p1173, @p1174, @p1175, @p1176, @p1177, @p1178, @p1179, @p1180, @p1181, @p1182, @p1183, @p1184, @p1185),
(@p1186, @p1187, @p1188, @p1189, @p1190, @p1191, @p1192, @p1193, @p1194, @p1195, @p1196, @p1197, @p1198, @p1199, @p1200, @p1201, @p1202, @p1203, @p1204, @p1205, @p1206, @p1207, @p1208, @p1209, @p1210),
(@p1211, @p1212, @p1213, @p1214, @p1215, @p1216, @p1217, @p1218, @p1219, @p1220, @p1221, @p1222, @p1223, @p1224, @p1225, @p1226, @p1227, @p1228, @p1229, @p1230, @p1231, @p1232, @p1233, @p1234, @p1235);";

        public StringSearchBenchmark()
        {
        }


        [Benchmark]
        public string IndexOfSearch()
        {
            var selectAffectedCountCommandText = new StringBuilder().AppendLine("SET NOCOUNT ON;");
            var startIndex = 0;
            var seekString = "SELECT @@ROWCOUNT;";
            while ((startIndex = commandText.IndexOf(seekString, startIndex,StringComparison.Ordinal)) != -1)
            {
                startIndex += 18;
                selectAffectedCountCommandText.AppendLine("SELECT 1;");
            }
            return selectAffectedCountCommandText.ToString();
        }

        [Benchmark]
        public string ReplaceSearch()
        {
            var seekString = "SELECT @@ROWCOUNT;";
            var count =  (commandText.Length - commandText.Replace(seekString, "", StringComparison.Ordinal).Length) / 18;
            var t = new StringBuilder().AppendLine("SET NOCOUNT ON;");
            for (int i = 0; i < count; i++)
            {
                t.AppendLine("SELECT 1;");
            }
            return t.ToString();
        }
    }
}
