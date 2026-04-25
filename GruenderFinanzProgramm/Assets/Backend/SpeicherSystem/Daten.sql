-- SQL Code beispiel.

--Befehl zum erstellen einer Datenbank
CREATE DATABASE Daten;
--Befehl zum verwenden einer Datenbank
USE Daten;

--Befehl zum erstellen einer Tabelle in der Datenbank.
CREATE TABLE Tabelle(
    Id INT PRIMARY KEY NOT NULL,
    Gewinn INT
);

-- Beispiel wie man Daten in die Tabelle einfügt.
INSERT INTO Tabelle (Id, Gewinn) 
VALUES (1, 1000);

-- Beispiel wie man Daten aus der Tabelle abfragt. hier werden alle Ids und Gewinne aus der Tabelle abgefragt.
SELECT id, Gewinn FROM Tabelle;