-- Create Fees table
CREATE TABLE IF NOT EXISTS `Fees` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ChildId` int NOT NULL,
    `Amount` decimal(10,2) NOT NULL,
    `Description` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `DueDate` datetime(6) NOT NULL,
    `PaidDate` datetime(6) NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'pending',
    `FeeType` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'monthly',
    `Notes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_Fees` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Fees_Children_ChildId` FOREIGN KEY (`ChildId`) REFERENCES `Children` (`Id`) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX `IX_Fees_ChildId_DueDate` ON `Fees` (`ChildId`, `DueDate`);
CREATE INDEX `IX_Fees_Status` ON `Fees` (`Status`);
CREATE INDEX `IX_Fees_DueDate` ON `Fees` (`DueDate`);