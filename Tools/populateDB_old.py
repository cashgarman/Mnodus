import pymongo
import random
import math
from bson.objectid import ObjectId

print "Running..."

# Settings
minSitesPerCompany = 1
maxSitesPerCompany = 10
minNumberEmailsPerPerson = 3
maxNumberEmailsPerson = 10
meanPersonAge = 35
personAgeSigma = 10
numCompanies = 123
importantEmailChance = 0.15
profitFactor = 1.5

companyHierarchy = [
	{
		"Job": "CEO",
		"Min": 1,
		"Max": 1,
		"MinSalary": 100000,
		"MaxSalary": 1000000,
		"Manages":
		[
			{
				"Job": "Manager",
				"Min": 1,
				"Max": 5,
				"MinSalary": 50000,
				"MaxSalary": 200000,
				"Manages":
				[
					{
						"Job": "Worker",
						"Min": 1,
						"Max": 10,
						"MinSalary": 25000,
						"MaxSalary": 100000,
					}
				]
			}
		]
	}
]

maleFirstNames = open("maleFirstNames.txt").readlines()
femaleFirstNames = open("femaleFirstNames.txt").readlines()
lastNames = open("lastNames.txt").readlines()
companyNames = open("companyNames.txt").readlines()
countryNames = open("lessCountryNames.txt").readlines()
industryNames = open("industryNames.txt").readlines()
emailSubjects = open("emailSubjects.txt").readlines()

countries = []
industries = []
companies = []
sites = []
people = []
emails = []

# Connect to the MongoDB database
# client = pymongo.MongoClient('mongodb://cashgarman:klaxon@ds061405.mongolab.com:61405/test_dataset?authMechanism=SCRAM-SHA-1')
# client = pymongo.MongoClient('mongodb://cashgarman:klaxon@ds061405.mongolab.com:61405/test_dataset')
client = pymongo.MongoClient('localhost', 27017)
db = client.test_dataset

# Clear out any existing data
client.drop_database('test_dataset')

def Lerp(min, max, amount):
	return min + (max - min) * amount

def FindEntityByID(entities, ID):
	for entity in entities:
		if entity["_id"] is ID:
			return entity
	print "Couldn't find entity with ID %s" % ID

def GetRandomCountry(countries):
   total = sum(c["scale"] for c in countries)
   r = random.uniform(0, total)
   upto = 0
   for c in countries:
      if upto + c["scale"] >= r:
         return c
      upto += c["scale"]
   assert False, "Shouldn't get here"

def CreateCountries():
	countries = []
	for countryName in countryNames:
		countries.append(
			{
				"_id": ObjectId(),
				"type": "Country",
				"name": countryName.strip(),
				"scale": random.random(),
				"companyIDs": [],
				"siteIDs": [],
				"numCompanies": 0,
				"numSites": 0,
				"totalRevenue": 0,
				"totalPeople": 0,
			})
	return countries

def CreateIndustries():
	industries = []
	for industryName in industryNames:
		industries.append(
			{
				"_id": ObjectId(),
				"type": "Industry",
				"name": industryName.strip(),
				"companyIDs": [],
				"numCompanies": 0,
				"numSites": 0,
				"totalRevenue": 0,
				"totalPeople": 0
			})
	return industries

def CreateCompanies():
	companies = []
	for companyName in companyNames[0:numCompanies]:
		industry = random.choice(industries)
		company = {
				"_id": ObjectId(),
				"type": "Company",
				"name": companyName.strip(),
				"scale": random.random(),
				"industryID": industry["_id"],
				"siteIDs": [],
				"numSites": 0,
				"countryIDs": set(),
				"totalRevenue": 0,
				"numEmployees": 0,
				"averageEmployeePerformance": 0
			}
		companies.append(company)
		industry["companyIDs"].append(company["_id"])
		industry["numCompanies"] += 1
	return companies

def CreateSites():
	sites = []
	for company in companies:
		numSites = int(Lerp(minSitesPerCompany, maxSitesPerCompany, company["scale"]))
		for i in range(0, numSites):
			country = GetRandomCountry(countries)
			site = {
					"_id": ObjectId(),
					"type": "Site",
					"companyID": company["_id"],
					"countryID": country["_id"],
					"industryID": company["industryID"],
					"name": "%s Site %d" % (company["name"], i + 1),
					"scale": random.random(),
					"numEmployees": 0,
					"totalRevenue": 0
				}
			sites.append(site)

			country["siteIDs"].append(site["_id"])
			company["siteIDs"].append(site["_id"])
			company["countryIDs"].add(country["_id"])
			if company["_id"] not in country["companyIDs"]:
				country["companyIDs"].append(company["_id"])
				country["numCompanies"] += 1
			company["numSites"] += 1
			country["numSites"] += 1
			industry = FindEntityByID(industries, company["industryID"])
			industry["numSites"] += 1

		company["countryIDs"] = list(company["countryIDs"])

	return sites

def CreatePerson(site, job, boss):
	people = []
	numJob = int(Lerp(job["Min"], job["Max"], site["scale"]))
	for i in range(0, numJob):
		isMale = random.choice([True, False])
		salary = int(math.floor(random.randint(job["MinSalary"], job["MaxSalary"]) / 1000) * 1000)
		company = FindEntityByID(companies, site["companyID"])
		person = {
				"_id": ObjectId(),
				"type": "Person",
				"name": "%s %s" % (random.choice(maleFirstNames).strip() if isMale else random.choice(femaleFirstNames).strip(),
					random.choice(lastNames).strip()),
				"isMale": isMale,
				"age": int(random.gauss(meanPersonAge, personAgeSigma)),
				"siteID": site["_id"],
				"companyID": company["_id"],
				"job": job["Job"],
				"bossID": boss["_id"] if boss is not None else "000000000000000000000000",
				"salary": salary,
				"performance": random.random(),
				"homeCountryID": FindEntityByID(countries, site["countryID"])["_id"],
				"industryID": company["industryID"]
			}
		people.append(person)
		site["numEmployees"] += 1

		if "Manages" in job:
			for managedJob in job["Manages"]:
				people += CreatePerson(site, managedJob, person)

		if job["Job"] is "CEO":
			for company in companies:
				if company["_id"] is site["companyID"]:
					company["CEOID"] = person["_id"]

	return people

def CreatePeople():
	people = []
	for site in sites:
		peopleOnSite = CreatePerson(site, companyHierarchy[0], None)

		site["peopleIDs"] = [p["_id"] for p in peopleOnSite]
		site["numEmployees"] = len(peopleOnSite)
		site["totalSalaries"] = sum([p["salary"] for p in peopleOnSite])
		site["averageEmployeePerformance"] = sum([p["performance"] for p in peopleOnSite]) / len(peopleOnSite)
		site["totalRevenue"] = site["totalSalaries"] * profitFactor * site["averageEmployeePerformance"]

		company = FindEntityByID(companies, site["companyID"])
		company["totalRevenue"] += site["totalRevenue"]
		company["numEmployees"] += site["numEmployees"]

		country = FindEntityByID(countries, site["countryID"])
		country["totalRevenue"] += site["totalRevenue"]
		country["totalPeople"] += site["numEmployees"]

		industry = FindEntityByID(industries, site["industryID"])
		industry["totalRevenue"] += site["totalRevenue"]
		industry["totalPeople"] += site["numEmployees"]

		people += peopleOnSite
		
	return people

def CreateEmails():
	emails = []
	for person in people:
		personsEmails = []
		numEmails = random.randint(minNumberEmailsPerPerson, maxNumberEmailsPerson)
		peopleOnSameSite = [otherPerson for otherPerson in people if otherPerson["siteID"] is person["siteID"] and otherPerson["_id"] is not person["_id"]]
		for i in range(0, numEmails):
			email = {
					"_id": ObjectId(),
					"type": "Email",
					"name": "%s%s" % ("RE: " if random.random() < 0.5 else "", random.choice(emailSubjects)),
					"senderID": random.choice(peopleOnSameSite)["_id"],
					"recipientID": person["_id"],
					"important": True if random.random() < importantEmailChance else False,
				}
			personsEmails.append(email)
			emails.append(email)
		person["emailIDs"] = [e["_id"] for e in personsEmails]
	return emails

# Create everything
countries = CreateCountries()
print "Created %d countries" % len(countries)

industries = CreateIndustries()
print "Created %d industries" % len(industries)

companies = CreateCompanies()
print "Created %d companies" % len(companies)

sites = CreateSites()
print "Created %d sites" % len(sites)

people = CreatePeople()
print "Created %d people" % len(people)

for country in countries:
	print "%s has %d people" % (country["name"], country["totalPeople"])
# emails = CreateEmails()
# print "Created %d emails" % len(emails)

# Add the countries
countriesCollection = db.countries
countriesCollection.remove()
countriesCollection.insert_many(countries)
print "Added %d countries" % countriesCollection.count()

# Add the industries
industriesCollection = db.industries
industriesCollection.remove()
industriesCollection.insert_many(industries)
print "Added %d industries" % industriesCollection.count()

# Add the companies
companiesCollection = db.companies
companiesCollection.remove()
companiesCollection.insert_many(companies)
print "Added %d companies" % companiesCollection.count()

# Add the sites
sitesCollection = db.sites
sitesCollection.remove()
sitesCollection.insert_many(sites)
print "Added %d sites" % sitesCollection.count()

# Add the people
peopleCollection = db.people
peopleCollection.remove()
peopleCollection.insert_many(people)
print "Added %d people" % peopleCollection.count()

# # Add the emails
# emailsCollection = db.emails
# emailsCollection.remove()
# emailsCollection.insert_many(emails)
# print "Added %d emails" % emailsCollection.count()

# print random.choice(emails)

# for industry in industries:
# 	print "%s has %d companies" % (industry["name"], len(industry["companyIDs"]))

client.close()