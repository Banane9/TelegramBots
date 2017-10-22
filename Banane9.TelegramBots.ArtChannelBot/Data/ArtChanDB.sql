CREATE TABLE IF NOT EXISTS Channels ( 
    Id        INTEGER      PRIMARY KEY AUTOINCREMENT
                           NOT NULL
                           UNIQUE,
    Name      CHAR( 255 )  NOT NULL
                           COLLATE 'NOCASE',
    ChannelId INTEGER      NOT NULL
                           UNIQUE 
);

CREATE TABLE IF NOT EXISTS Characters ( 
    Id   INTEGER PRIMARY KEY AUTOINCREMENT
                 NOT NULL
                 UNIQUE,
    Name CHAR    NOT NULL
                 UNIQUE
                 COLLATE 'NOCASE' 
);

CREATE TABLE IF NOT EXISTS Artists ( 
    Id   INTEGER PRIMARY KEY AUTOINCREMENT
                 NOT NULL
                 UNIQUE,
    Name CHAR    NOT NULL
                 UNIQUE
                 COLLATE 'NOCASE' 
);

CREATE TABLE IF NOT EXISTS Tags ( 
    Id   INTEGER PRIMARY KEY AUTOINCREMENT
                 NOT NULL
                 UNIQUE,
    Name CHAR    NOT NULL
                 UNIQUE
                 COLLATE 'NOCASE' 
);

CREATE TABLE IF NOT EXISTS Art ( 
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT
                                NOT NULL
                                UNIQUE,
    ChannelId           INTEGER NOT NULL
                                REFERENCES Channels ( Id ),
    FileId              CHAR    NOT NULL
                                UNIQUE,
    DetailMessageChatId INTEGER NOT NULL,
    DetailMessageId     INTEGER NOT NULL,
    Name                CHAR    NOT NULL
                                COLLATE 'NOCASE',
    Rating              CHAR    NOT NULL
                                COLLATE 'NOCASE',
    UNIQUE ( DetailMessageChatId, DetailMessageId ) 
);

CREATE TABLE IF NOT EXISTS ArtTags ( 
    ArtId INTEGER NOT NULL
                  REFERENCES Art ( Id ) ON DELETE CASCADE,
    TagId INTEGER NOT NULL
                  REFERENCES Tags ( Id ) ON DELETE CASCADE,
    UNIQUE(ArtId, TagId)
);

CREATE TABLE IF NOT EXISTS ArtistPieces ( 
    ArtId    INTEGER NOT NULL
                     REFERENCES Art ( Id ) ON DELETE CASCADE,
    ArtistId INTEGER NOT NULL
                     REFERENCES Artists ( Id ) ON DELETE CASCADE,
    UNIQUE(ArtId, ArtistId)
);

CREATE TABLE IF NOT EXISTS CharacterArt ( 
    ArtId       INTEGER NOT NULL
                        REFERENCES Art ( Id ) ON DELETE CASCADE,
    CharacterId INTEGER NOT NULL
                        REFERENCES Characters ( Id ) ON DELETE CASCADE,
    UNIQUE(ArtId, CharacterId)
);

CREATE TABLE IF NOT EXISTS Users ( 
    Id     INTEGER PRIMARY KEY AUTOINCREMENT
                   NOT NULL
                   UNIQUE,
    UserId INTEGER NOT NULL
                   UNIQUE
);

CREATE TABLE IF NOT EXISTS UserSubscribedChannels ( 
    UserId    INTEGER NOT NULL
                      REFERENCES Users ( Id ) ON DELETE CASCADE,
    ChannelId INTEGER NOT NULL
                      REFERENCES Channels ( Id ) ON DELETE CASCADE,
    UNIQUE ( UserId, ChannelId ) 
);