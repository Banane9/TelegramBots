
-- Table: Channels
CREATE TABLE IF NOT EXISTS Channels ( 
    Id        INTEGER      PRIMARY KEY AUTOINCREMENT
                           NOT NULL
                           UNIQUE,
    Name      CHAR( 255 )  NOT NULL
                           COLLATE 'NOCASE',
    ChannelId INTEGER      NOT NULL
                           UNIQUE 
);


-- Table: Characters
CREATE TABLE IF NOT EXISTS Characters ( 
    Id   INTEGER PRIMARY KEY AUTOINCREMENT
                 NOT NULL
                 UNIQUE,
    Name CHAR    NOT NULL
                 UNIQUE
                 COLLATE 'NOCASE' 
);


-- Table: Artists
CREATE TABLE IF NOT EXISTS Artists ( 
    Id   INTEGER PRIMARY KEY AUTOINCREMENT
                 NOT NULL
                 UNIQUE,
    Name CHAR    NOT NULL
                 UNIQUE
                 COLLATE 'NOCASE' 
);


-- Table: Tags
CREATE TABLE IF NOT EXISTS Tags ( 
    Id   INTEGER PRIMARY KEY AUTOINCREMENT
                 NOT NULL
                 UNIQUE,
    Name CHAR    NOT NULL
                 UNIQUE
                 COLLATE 'NOCASE' 
);


-- Table: Art
CREATE TABLE IF NOT EXISTS Art ( 
    Id        INTEGER PRIMARY KEY AUTOINCREMENT
                      NOT NULL
                      UNIQUE,
    ChannelId INTEGER NOT NULL
                      REFERENCES Channels ( Id ),
    MessageId INTEGER NOT NULL,
    FileId    CHAR    NOT NULL,
    Name      CHAR    NOT NULL
                      COLLATE 'NOCASE',
    Rating    CHAR    NOT NULL
                      COLLATE 'NOCASE',
    UNIQUE(ChannelId, MessageId)
);


-- Table: ArtTags
CREATE TABLE IF NOT EXISTS ArtTags ( 
    Id    INTEGER PRIMARY KEY AUTOINCREMENT
                  NOT NULL
                  UNIQUE,
    ArtId INTEGER NOT NULL
                  REFERENCES Art ( Id ) ON DELETE CASCADE,
    TagId INTEGER NOT NULL
                  REFERENCES Tags ( Id ) ON DELETE CASCADE,
    UNIQUE(ArtId, TagId)
);


-- Table: ArtistPieces
CREATE TABLE IF NOT EXISTS ArtistPieces ( 
    Id       INTEGER PRIMARY KEY AUTOINCREMENT
                     NOT NULL
                     UNIQUE,
    ArtId    INTEGER NOT NULL
                     REFERENCES Art ( Id ) ON DELETE CASCADE,
    ArtistId INTEGER NOT NULL
                     REFERENCES Artists ( Id ) ON DELETE CASCADE,
    UNIQUE(ArtId, ArtistId)
);


-- Table: CharacterArt
CREATE TABLE IF NOT EXISTS CharacterArt ( 
    Id          INTEGER PRIMARY KEY AUTOINCREMENT
                        NOT NULL
                        UNIQUE,
    ArtId       INTEGER NOT NULL
                        REFERENCES Art ( Id ) ON DELETE CASCADE,
    CharacterId INTEGER NOT NULL
                        REFERENCES Characters ( Id ) ON DELETE CASCADE,
    UNIQUE(ArtId, CharacterId)
);

