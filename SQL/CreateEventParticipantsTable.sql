CREATE TABLE Events (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(50) NOT NULL,
    Description VARCHAR(500) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    AgeFrom INT NOT NULL,
    AgeTo INT NOT NULL,
    Capacity INT NOT NULL,
    Time VARCHAR(50) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL
);

CREATE TABLE EventParticipants (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventId INT NOT NULL,
    ChildId INT NOT NULL,
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RegisteredBy VARCHAR(450) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Registered',
    Notes VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    
    CONSTRAINT FK_EventParticipants_Event FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
    CONSTRAINT FK_EventParticipants_Child FOREIGN KEY (ChildId) REFERENCES Children(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_EventParticipants_EventChild UNIQUE (EventId, ChildId)
);

CREATE INDEX IX_EventParticipants_EventId ON EventParticipants(EventId);
CREATE INDEX IX_EventParticipants_ChildId ON EventParticipants(ChildId);
CREATE INDEX IX_EventParticipants_RegisteredBy ON EventParticipants(RegisteredBy);