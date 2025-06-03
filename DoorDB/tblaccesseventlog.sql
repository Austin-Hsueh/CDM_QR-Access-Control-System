CREATE TABLE `tblaccesseventlog` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NodeId` int NOT NULL,
  `FunctionCode` int NOT NULL,
  `EventTime` datetime(6) NOT NULL,
  `PortNumber` int NOT NULL,
  `UserAddress` int NOT NULL,
  `DoorNumber` int NOT NULL,
  `CardId1` int NOT NULL,
  `CardId2` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_EventTime` (`EventTime`),
  KEY `IX_UserAddress` (`UserAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;