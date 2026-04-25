-- SQL Code beispiel.

--Befehl zum erstellen einer Datenbank
create Database Daten;
--Befehl zum verwenden einer Datenbank
use Daten;

--Befehl zum erstellen einer Tabelle in der Datenbank.
create table Tabelle(
    Id INT PRIMARY KEY NOT NULL,
    Gewinn INT
);

-- Beispiel wie man Daten in die Tabelle einfügt.
INSERT INTO Tabelle (Id, Gewinn) 
VALUES (1, 1000);

-- Beispiel wie man Daten aus der Tabelle abfragt. hier werden alle Ids und Gewinne aus der Tabelle abgefragt.
SELECT id, Gewinn FROM Tabelle;