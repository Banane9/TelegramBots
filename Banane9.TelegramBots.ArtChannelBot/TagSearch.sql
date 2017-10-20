SELECT
    Channels.ChannelId AS ChannelId,
    Channels.Name AS ChannelName,
    Art.Id AS ArtId,
    Art.MessageId AS MessageId,
    Art.FileId AS FileId,
    Art.Name AS ArtName
FROM Art
INNER JOIN Channels ON Channels.Id = Art.ChannelId
WHERE EXISTS
    (SELECT ArtId FROM ArtTags WHERE
        ArtId = Art.Id AND
        EXISTS (SELECT * FROM Tags
            	WHERE (ArtTags.TagId = Tags.Id)
                AND (Tags.Name LIKE 'fo%')))
OR EXISTS
    (SELECT ArtId FROM ArtistPieces WHERE
        ArtId = Art.Id AND
        EXISTS (SELECT * FROM Artists
            WHERE (ArtistPieces.ArtistId = Artists.Id)
                AND (Artists.Name LIKE 'tes%')))
OR EXISTS
    (SELECT ArtId FROM CharacterArt WHERE
        ArtId = Art.Id AND
        EXISTS (SELECT * FROM Characters
            WHERE (CharacterArt.CharacterId = Characters.Id)
                AND (Characters.Name LIKE 'kau%')))
