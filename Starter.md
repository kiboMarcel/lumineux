Une desciption basique du projet lumineux

Ce fichier fournit une description tres basique de l'ensemble du projet qui te guidera a produire une structure solide bien detaillé
de tout les projets. alors n'hesite pas a posée des questions pour bien comprendre tout le projet enfin de produire 
une documentation bien detaillé (business logique, architecture, layers, base de donné...) sur lequel batir le projet vraiment scallable(Spec Driven Developpement)

c'est un service qui a plusieurs fonctions tu pourras de referer au fichier Database Entities Documentation.md pour avoir une idée 
genrale de quoi devrait resemblé le projet.

c'est une grande application qui fait plusieurs choses.
un service pour gerer toutes les actions de la communauté Lumineux.



Architecture
 - Une BD en SQL server
 - un APi en .net core
    qui a son propre architecture - en Onion avec les couches bien separé ainsi que les tests unitaires.
 - un SPA en angular 
 - une applicationm mobile en flutter

 le SPA fera office de dashboard vraiment complet pour la gestion 
 l'application mobile aura aussi un module dashborad mais pas vraiment complet pour amelioré l'experience utlisateur (tu me donnera ton avis)

- on commencera par mettre en place en premier l'API. ca sera du code First.


cette communuaté a : 
 - des membres  (nom, prenom,sexe, fonction, age,contact, quartier, pays d'origine, pays de residence ...)
 - bureau qui composé des personnes a qui sont assigné des profils (qui peuvent changer) qui ont des droits d'acces a 
differents niveau de l'application (affichage, lecture...)
 - des antennes : sont les lieux ou les reunions sont faites (chaque antennes a des membres qui lui sont attribué, mais un membre peut aller a une antenne differentes de sont antennes d'origine pour)

- c'est le bureau qui s'occupe d'ajouter un nouveau membre, ainsi donc un compte est crée pour ce membre avec les inforamtion basique
il pourra se connecter avec les identifiant puis apres mettre son nouveau mot de passe et faire les updates necessaires sur les champs
sur lesquelle il a droit

la gestion de presence des membres (qui sera le premier feature en mettre en place)
a chaque reunion qui se fera a une antenne une session sera demarré par un membre du bureau qui aura les informations necesseaire 
date (annee moi jour heure) chaque membre de la communauté qui a l'application mobile son arrivé pour scanné un code QR genreé par cette session le membre sera donc enregisté le moment du scan commme heure d'arrivé, le membre du bureau a la possibilité d'ajouter aussi les presence des personné qui n'on pas l'application mobile. l'heure ou la session terminié est l'heure qui sera enregistré comme fin de la reunion et sera donc renseigné pour tous les presonne qui a scané le QR code ou on ete ajoute par l'admin
 
