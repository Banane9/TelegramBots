INSERT INTO ArtSearchResults
SELECT
    Channels.ChannelId AS ChannelId,
    Channels.Name AS ChannelName,
    Channels.JoinLink AS ChannelJoinLink,
    Art.Id AS ArtId,
    Art.FileId AS FileId,
    Art.Name AS ArtName
FROM Art
INNER JOIN Channels ON Channels.Id = Art.ChannelId
WHERE Art.ChannelId IN (SELECT ChannelId FROM UserSubscribedChannels WHERE (UserSubscribedChannels.UserId = @userId)) AND (
Art.Name LIKE @term
OR EXISTS
    (SELECT 1 FROM ArtTags WHERE
        ArtTags.ArtId = Art.Id AND
        EXISTS (SELECT * FROM Tags
                WHERE (ArtTags.TagId = Tags.Id)
                AND (Tags.Name LIKE @term)))
OR EXISTS
    (SELECT 1 FROM ArtistPieces WHERE
        ArtistPieces.ArtId = Art.Id AND
        EXISTS (SELECT * FROM Artists
            WHERE (ArtistPieces.ArtistId = Artists.Id)
                AND (Artists.Name LIKE @term)))
OR EXISTS
    (SELECT 1 FROM CharacterArt WHERE
        CharacterArt.ArtId = Art.Id AND
        EXISTS (SELECT * FROM Characters
            WHERE (CharacterArt.CharacterId = Characters.Id)
                AND (Characters.Name LIKE @term)))
)