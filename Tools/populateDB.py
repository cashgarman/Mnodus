import sys
import json
import pymongo
import random
import math
from bson.objectid import ObjectId

# TESTING
sys.argv = ["", "../Assets/Resources/DatasetsJSON/mining_2.json", "test_dataset"]

if len(sys.argv) < 3:
	print("populateDB.py <DATASET_PATH> <DATABASE_NAME>")
	exit()

datasetPath = sys.argv[1]
databaseName = sys.argv[2]

print("Loading %s into MongoDB %s..." % (datasetPath, databaseName))

dataset = json.load(open(datasetPath))

def Lerp(min, max, amount):
	return min + (max - min) * amount

def FindEntityByID(entities, ID):
	for entity in entities:
		if entity["_id"] is ID:
			return entity
	print("Couldn't find entity with ID %s" % ID)

# Connect to the database
client = pymongo.MongoClient('localhost', 27017)

# Clear out any existing data
client.drop_database(databaseName)

db = client.test_dataset
db = getattr(client, databaseName)

entityTypes = []
for node in dataset["nodes"]:
	if node["type"] not in entityTypes:
		entityTypes.append(node["type"])
		db.create_collection(node["type"])
		print("Found new entity type %s" % node["type"])
	newNode = db.get_collection(node["type"]).insert_one(node)

for entityType in entityTypes:
	print("Saved %d of type %s" % (db.get_collection(entityType).count(), entityType))

client.close()