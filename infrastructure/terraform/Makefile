tf-init:
	rm -rf .terraform
	az account set -s "Pay-As-You-Go"  
	terraform init

tf-plan:
	az account set -s "Pay-As-You-Go" 
	terraform fmt
	terraform plan

tf-apply:
	az account set -s "Pay-As-You-Go" 
	terraform fmt
	terraform apply

tf-destroy:
	az account set -s "Pay-As-You-Go" 
	terraform destroy
