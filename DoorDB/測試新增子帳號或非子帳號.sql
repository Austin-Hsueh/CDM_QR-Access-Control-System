INSERT INTO tblAccessEventLog (
    Id,
    NodeId,
    FunctionCode,
    EventTime,
    PortNumber,
    UserAddress,
    DoorNumber,
    CardId1,
    CardId2
) VALUES (
    UUID(),           -- 產生唯一ID
    1,               -- NodeId
    1,               -- FunctionCode
    NOW(),           -- EventTime (現在時間)
    1,               -- PortNumber
    53,              -- UserAddress = 51
    1,               -- DoorNumber = 1 (大門)
    0,               -- CardId1
    0                -- CardId2
);